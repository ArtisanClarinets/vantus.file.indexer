# Policy Management

Vantus File Indexer supports "Managed Mode" for enterprise environments. Policies allow administrators to enforce specific setting values and prevent user modification.

## Mechanism
Policies are defined in a JSON file (e.g., `policies.json`) or distributed via MDM/Registry.

When a setting is locked:
1. The application forces the value defined in the policy.
2. The UI controls for that setting are disabled.
3. A lock icon and reason are displayed to the user.

## Policy File Format

```json
{
  "managed": true,
  "locks": [
    {
      "setting_id": "privacy.encrypt_index_db",
      "locked_value": true,
      "reason": "Encryption is required by organization policy.",
      "source": "MDM"
    },
    {
      "setting_id": "data.telemetry",
      "locked_value": false,
      "reason": "Data collection is disabled for privacy compliance.",
      "source": "Group Policy"
    }
  ]
}
```

## Fields
- `managed` (bool): Master switch for managed mode.
- `locks` (array): List of locks.
  - `setting_id`: ID of the setting to lock (must match `settings_definitions.json`).
  - `locked_value`: The value to enforce (must match type).
  - `reason`: Text displayed to the user.
  - `source`: Origin of the policy (informational).

## Locked vs Default
- A **Preset** provides a default value that the user can change.
- A **Policy** provides a forced value that the user *cannot* change.
