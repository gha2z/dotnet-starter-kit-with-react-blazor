namespace FSH.BlazorShared.Components;

/// <summary>One selectable row in <see cref="FshCombobox"/>. <c>Value</c> is the opaque id (string); <c>Label</c> is displayed.</summary>
public sealed record FshComboboxOption(string? Value, string Label);
