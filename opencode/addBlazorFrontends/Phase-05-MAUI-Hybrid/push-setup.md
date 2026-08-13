# Push notifications — enablement guide (Phase 5.4)

The hybrid app ships a **compile-gated** push skeleton: `IPushNotificationService` /
`PushNotificationService` are registered and callable today, but on non-Android builds (or
without the `FSH_FIREBASE` define) `RegisterAsync()` returns `null` and logs a message.
Nothing is wired to Firebase yet because it needs **external setup** from you:

1. **Firebase project + `google-services.json`**
   - Create a Firebase project (or reuse an existing one) at https://console.firebase.google.com
   - Add an **Android app** with the hybrid app's package id (default: `com.companyname.fsh.hybrid` —
     check `Platforms\Android\AndroidManifest.xml`).
   - Download `google-services.json` and place it at `clients\FSH.Hybrid\FSH.Hybrid\Platforms\Android\google-services.json`
     (build action `GoogleServicesJson` — set automatically by the plugin build target).

2. **Add the Firebase Messaging plugin** (one-time, in `clients\FSH.Hybrid\FSH.Hybrid\FSH.Hybrid.csproj`):
   ```xml
   <PackageReference Include="Plugin.Firebase.CloudMessaging" Version="2.x" />
   ```
   and initialize the plugin in `MauiProgram.cs` / `MainActivity` per the plugin README
   (`CrossFirebaseCloudMessaging.Current.Initialize(...)`).

3. **Enable the gated path** — add `<DefineConstants>$(DefineConstants);FSH_FIREBASE</DefineConstants>`
   to the Android `PropertyGroup` in the csproj. The `#if ANDROID && FSH_FIREBASE` branch in
   `Services\PushNotificationService.cs` then requests `POST_NOTIFICATIONS` (manifest permission
   is already declared) and returns the FCM token.

4. **Backend sender** — FSH has no push sender yet; the Files/Notifications modules are
   webhook-based. Planned: a Hangfire job per tenant that POSTs to
   `https://fcm.googleapis.com/v1/projects/{project}/messages:send` (OAuth2 service-account
   token) whenever a notification is created. That backend work is **out of scope for Phase 5**
   and tracked in the plan; the mobile side above is the only mobile work needed.

## Verification checklist
- [ ] `google-services.json` present (never commit it — it's in `.gitignore`)
- [ ] Android build succeeds with the plugin + define
- [ ] On a device: `RegisterAsync()` returns a 150+ char token; FCM console "Test message" arrives
- [ ] Token persisted (planned: `SecureStorage` on registration) and sent to the backend sender
