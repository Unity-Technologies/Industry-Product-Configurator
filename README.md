# Industry Product Configurator

![2024-05-09 10-26-30_trimmed_high](https://github.com/Unity-Technologies/Industry-Product-Configurator/assets/89197200/0ac38309-6113-45a9-a32a-dba085205a97)

This is a lightweight product configurator framework designed for Unity. It empowers users to effortlessly create their own product configurator experiences within Unity environments. With a set of foundational classes, users can seamlessly expand and customize their configurators to suit their specific needs and preferences.

Currently, our framework supports visibility, material, animation and transform variants. This means users can easily configure product options related to visibility (show/hide), material properties, and transformations (such as position, rotation, and scale). Whether you're building a simple configurator for basic products or a complex one with intricate customization options, our framework provides the flexibility and ease-of-use necessary to bring your vision to life.

## Installation
For the most up-to-date and reliable installation process, we recommend following the official documentation provided by Unity. The steps outlined in the official documentation ensure a smooth integration and compatibility with your Unity project as they incorporate the latest updates and best practices.

Please refer to the [official Unity documentation](https://docs.unity3d.com/Manual/upm-ui-giturl.html) for detailed instructions on how to install packages via UPM.


1. Copy the URL of the GitHub Repo:
<img width="480" alt="Screenshot 2024-05-03 at 11 40 42" src="https://github.com/Unity-Technologies/Industry-Product-Configurator/assets/89197200/be54e9ac-058c-4485-9e9f-a558317c99c6">


2. Open Unity: Launch Unity and open your project.
3. Open Package Manager: Navigate to "Window" > "Package Manager" to open the Unity Package Manager.
<img width="480" alt="Screenshot 2024-05-03 at 11 43 41" src="https://github.com/Unity-Technologies/Industry-Product-Configurator/assets/89197200/6893c92d-137d-492e-9df8-15ea0a3e02ef">

4. Add Package from Git URL: Click on the "+" button in the top-left corner of the Package Manager window and select "Add package from git URL...".
<img width="480" alt="Screenshot 2024-05-03 at 11 53 38" src="https://github.com/Unity-Technologies/Industry-Product-Configurator/assets/89197200/aebedd9b-f197-4dba-bb0f-c37ef76bffc3">

5. Enter Git URL: Paste the Git URL of this package into the text field and press Enter.
<img width="480" alt="Screenshot 2024-05-03 at 11 55 01" src="https://github.com/Unity-Technologies/Industry-Product-Configurator/assets/89197200/922fab4f-5465-443f-a72f-5c05e1de207a">


## Basic Sample Included
<img width="480" alt="Screenshot 2024-05-03 at 12 01 00" src="https://github.com/Unity-Technologies/Industry-Product-Configurator/assets/89197200/7a2a63c4-73c2-4bc0-907e-74bcb647bbd4">

A basic sample demonstrating the usage of this package is provided within the package itself. For a quick start and understanding of the package functionalities, we recommend exploring the provided sample. By following the sample, users can gain insights into how the package works and begin implementing their own product configurator experiences with ease.


## Getting Started
To quickly get started with our product configurator framework, follow these simple steps:

### Create Variant Set
1. Access Variant Set Creation: Right-click in the Hierarchy panel to open the context menu.
2. Select Product Configurator: Navigate to "Product Configurator" > "Variant Set" in the context menu.
3. Choose Variant Set Type: Choose the type of variant set you want to create. For example, select "GameObject Variant Set" to create a variant set based on different game objects.
<img width="480" alt="Screenshot 2024-05-03 at 12 06 05" src="https://github.com/Unity-Technologies/Industry-Product-Configurator/assets/89197200/06d018d4-ebc7-4ed2-a39e-686ac0424c6e">

### Moditfy Variant Set
1. Select the new created variant set and go to Inspector
2. Name your Variant Set and hit create. This will create a new asset in the project that stores the ID and name, etc.
<img width="480" alt="Screenshot 2024-05-03 at 12 22 22" src="https://github.com/Unity-Technologies/Industry-Product-Configurator/assets/89197200/c88bf75c-9ad5-4b80-8931-691ee152eef6">

3. Name your variant and hit create, it will create a new variant along with a new asset that store information about this variant, e.g. ID, name and icon.
<img width="480" alt="Screenshot 2024-05-03 at 12 35 59" src="https://github.com/Unity-Technologies/Industry-Product-Configurator/assets/89197200/6fc66875-1ebb-45a6-a3d1-f14f4fae4231">

4. Expand the variants list, drag your variant object (in this case it is "Variant GameObject").
<img width="480" alt="Screenshot 2024-05-03 at 12 45 53" src="https://github.com/Unity-Technologies/Industry-Product-Configurator/assets/89197200/d547b582-85e8-45e6-a609-f8d3ea57309a">

5. Follow the step above to create another variant.
<img width="480" alt="Screenshot 2024-05-03 at 12 48 01" src="https://github.com/Unity-Technologies/Industry-Product-Configurator/assets/89197200/dadc307c-0889-44a2-90d1-1a96f21a0ea8">

6. Now this is how you setup your variant set

### Variant Slider
When a user has multiple variants, a slider will be displayed in the inspector, enabling them to toggle between each variant by dragging the slider handle.

<img width="480" alt="Screenshot 2024-05-03 at 12 54 06" src="https://github.com/Unity-Technologies/Industry-Product-Configurator/assets/89197200/9513f321-af69-437d-b71a-e28e8dbdbab5">

### Variant Icon Tool
<img width="480" alt="Screenshot 2024-05-03 at 12 55 19" src="https://github.com/Unity-Technologies/Industry-Product-Configurator/assets/89197200/b6c6436d-0595-4b62-8ce2-e9945f0d3ea1">

There is a tool available that facilitates the capture of icons for each variant using the scene view. This tool automatically stores the captured icons and assigns them to the corresponding variant asset files, streamlining the process of managing variant assets.

### Material Assignment
<img width="480" alt="Screenshot 2024-05-03 at 13 37 48" src="https://github.com/Unity-Technologies/Industry-Product-Configurator/assets/89197200/90b7ff3a-63fc-42c4-8216-35c8111d8510">

When utilizing the Material Variant Set, a tool is available to streamline the process. By inputting the parent object and a target material, this tool iterates through all child objects equipped with a MeshRenderer component. It identifies all instances where the material matches the target material, collecting the necessary indices and information. This enables seamless material changes when switching variants.

### Package Settings
A new settings page to make the package even more customizable. You can find it under "Edit" > "Project Settings" > "Product Configurator". Inside this settings page, there are various options that you can modify to better suit your specific needs. Explore these settings to fine-tune the configurator to your exact requirements.

### Advanced Mode
Within the settings page, there is a toggle to allow users to turn on "Advanced Mode". The difference between Basic Mode and Advanced Mode is as follows:

Basic Mode: Hides many fields in the inspector to provide a cleaner, simpler interface for users who do not need advanced customization options.
Advanced Mode: Exposes more fields in the inspector, giving advanced users greater control and flexibility to modify settings and configurations.
Explore these settings to fine-tune the configurator to your exact requirements.

### VariantSelect Component
The package includes a "VariantSelect" component that generates a dropdown menu of the existing variant sets. This feature is designed for non-developers to easily pick variant sets in the scene. The same functionality applies to the Variant field.

Additionally, there's a new "Variant Select" public function that users can invoke using UnityEvent. This is particularly useful for users who utilize the uGUI solution, allowing them to directly assign the component to the "OnClick" event.