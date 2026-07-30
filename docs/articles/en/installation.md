# Installation

Install Alchemy in your project using one of the following methods.

## Requirements

* Unity 2021.2 or later (Unity 2022.1 or later recommended for serialization extensions)
* Unity.Serialization 2.0 or later (if using serialization extensions)

## Install via Package Manager (Recommended)

You can install Alchemy via the Package Manager.

1. Open the Package Manager by navigating to Window > Package Manager.
2. Click the "+" button and choose "Add package from git URL".
3. Enter the following URL:

```text
https://github.com/annulusgames/Alchemy.git?path=/Alchemy/Assets/Alchemy
```

![img1](../../images/img-setup-1.png)

Alternatively, you can add the following line to the dependencies block in your Packages/manifest.json file:

```json
{
    "dependencies": {
        "com.annulusgames.alchemy": "https://github.com/annulusgames/Alchemy.git?path=/Alchemy/Assets/Alchemy"
    }
}
```

## Install from a `.unitypackage` File

You can also install Alchemy from a `.unitypackage` file.

1. Open the latest release on the Releases page.
2. Download the `.unitypackage` file.
3. Open the file and import it into your project.
