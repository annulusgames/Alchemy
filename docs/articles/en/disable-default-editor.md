# Disabling the Default Editor

By default, Alchemy uses its own editor class to render supported types. You can disable this behavior to avoid conflicts with other libraries or assets.

To disable Alchemy's default editor, add `ALCHEMY_DISABLE_DEFAULT_EDITOR` to the `Scripting Define Symbols` field under `Project Settings > Player`. To continue using Alchemy features while this symbol is defined, create a custom editor class that inherits from `AlchemyEditor`.
