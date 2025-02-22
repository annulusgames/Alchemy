# Installation

Let's install Alchemy into your project to start using it.

### Requirements

* Unity 2021.2 or later (Unity 2022.1 or later recommended for serialization extensions)
* Serialization 2.0 or later (if using serialization extensions)

### Install via Package Manager (Recommended)

You can install Alchemy via the Package Manager.

1. Open the Package Manager by navigating to Window > Package Manager.
2. Click the "+" button and choose "Add package from git URL".
3. Enter the following URL:

```text
https://github.com/yn01-dev/Alchemy.git?path=/Alchemy/Assets/Alchemy
```

![img1](../../images/img-setup-1.png)

Alternatively, you can add the following line to the dependencies block in your Packages/manifest.json file:

```json
{
    "dependencies": {
        "com.annulusgames.alchemy": "https://github.com/yn01-dev/Alchemy.git?path=/Alchemy/Assets/Alchemy"
    }
}
```

### Install from unitypackage

You can also install Alchemy from a unitypackage file.

1. Go to Releases and navigate to the latest release.
2. Download the unitypackage file.
3. Open the file and import it into your project.
