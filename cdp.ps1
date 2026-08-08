param(
    [string]$WebSocketUrl,
    [string]$Expression,
    [string]$Method = "Runtime.evaluate",
    [string]$ParamsJson = ""
)
$ws = [System.Net.WebSockets.ClientWebSocket]::new()
$ws.ConnectAsync([Uri]$WebSocketUrl, [Threading.CancellationToken]::None).GetAwaiter().GetResult()
if ($Method -eq "Runtime.evaluate") {
    $params = @{ expression = $Expression; returnByValue = $true; awaitPromise = $true }
} else {
    $params = $ParamsJson | ConvertFrom-Json -AsHashtable
}
$message = @{ id = 1; method = $Method; params = $params } | ConvertTo-Json -Depth 8 -Compress
$bytes = [Text.Encoding]::UTF8.GetBytes($message)
$ws.SendAsync([ArraySegment[byte]]::new($bytes), [Net.WebSockets.WebSocketMessageType]::Text, $true, [Threading.CancellationToken]::None).GetAwaiter().GetResult()
$buffer = New-Object byte[] 131072
$result = $ws.ReceiveAsync([ArraySegment[byte]]::new($buffer), [Threading.CancellationToken]::None).GetAwaiter().GetResult()
[Text.Encoding]::UTF8.GetString($buffer, 0, $result.Count)
$ws.Dispose()
