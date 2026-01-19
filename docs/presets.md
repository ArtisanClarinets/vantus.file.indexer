# Presets

Vantus File Indexer includes four standard presets designed for different use cases. Presets configure all settings to a known state.

## 1. Personal
**Target User**: Home users, students.
**Focus**: Usability, low impact, privacy.
- **Indexing**: Low priority, real-time detection.
- **Privacy**: Local storage, optional cloud features.
- **AI**: Basic models (CPU friendly).
- **Automation**: Gentle (Safety on).

## 2. Pro
**Target User**: Power users, developers, creatives.
**Focus**: Performance, deep indexing, automation.
- **Indexing**: Normal priority, full content indexing (code, images).
- **Privacy**: Local storage.
- **AI**: Advanced models (GPU enabled if available).
- **Automation**: Aggressive (Copy/Move enabled).

## 3. Enterprise-Private
**Target User**: Corporate environments with strict privacy needs.
**Focus**: Security, compliance, lock-down.
- **Indexing**: Scheduled or throttled.
- **Privacy**: Encrypted DB, PII masking enabled, Telemetry off.
- **AI**: CPU only or restricted models.
- **Cloud**: Disabled.

## 4. Enterprise-Automation
**Target User**: Server-side processing, automated workflows.
**Focus**: Throughput, headless operation.
- **Indexing**: High priority, large file limits.
- **Privacy**: Encrypted.
- **AI**: GPU accelerated.
- **Automation**: Full move/organize, no confirmation prompts.

## Behavior
- Applying a preset overwrites all unlock settings.
- Users can customize settings after applying a preset.
- Locked settings (Policy) always override presets.
