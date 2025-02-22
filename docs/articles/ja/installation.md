# インストール

プロジェクトにAlchemyをインストールして使用を開始しましょう。

### 要件

* Unity 2021.2 以上 (シリアル化拡張を使用する場合、2022.1以上を推奨)
* Serialization 2.0 以上 (シリアル化拡張を使用する場合)

### Package Manager経由でインストール (推奨)

Package Managerを利用してAlchemyをインストールできます。

1. Window > Package ManagerからPackage Managerを開く
2. 「+」ボタン > Add package from git URL
3. 以下のURLを入力する

```text
https://github.com/yn01-dev/Alchemy.git?path=/Alchemy/Assets/Alchemy
```

![img1](../../images/img-setup-1.png)

あるいはPackages/manifest.jsonを開き、dependenciesブロックに以下を追記します。

```json
{
    "dependencies": {
        "com.annulusgames.alchemy": "https://github.com/yn01-dev/Alchemy.git?path=/Alchemy/Assets/Alchemy"
    }
}
```

### unitypackageからインストール

配布されているunitypackageファイルからAlchemyをインストールすることも可能です。

1. Releasesから最新のリリースに移動
2. unitypackageファイルをダウンロード
3. ファイルを開き、プロジェクトにインポートする