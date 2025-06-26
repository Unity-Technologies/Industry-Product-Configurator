using UnityEngine;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;

namespace Unity.IndustryCSE.ProductConfigurator.Shared.Editor
{
    [InitializeOnLoad]
    public class AppUIInstaller
    {
        static ListRequest listRequest;
        static AddRequest addRequest;
        private static string packageName = "com.unity.dt.app-ui";

        static AppUIInstaller()
        {
            listRequest = Client.List();
            EditorApplication.update += CheckForAppUIInstallation;
        }

        private static void CheckForAppUIInstallation()
        {
            if(!listRequest.IsCompleted || listRequest.Status == StatusCode.Failure) return;
            if (listRequest.Status == StatusCode.Success)
            {
                bool foundPackage = false;
                foreach (var package in listRequest.Result)
                {
                    if(package.name != packageName) continue;
                    foundPackage = true;
                    break;
                }

                if (!foundPackage)
                {
                    if (EditorUtility.DisplayDialog("Missing Package", $"The required package '{packageName}' is not installed. Would you like to install it now?", "Install", "Cancel"))
                    {
                        addRequest = Client.Add(packageName);
                        EditorApplication.update += InstallPackage;
                    }
                }
            }
            EditorApplication.update -= CheckForAppUIInstallation;
        }

        private static void InstallPackage()
        {
            if (!addRequest.IsCompleted)
                return;

            if (addRequest.Status == StatusCode.Success)
                Debug.Log($"Successfully installed {addRequest.Result.packageId}");
            else
                Debug.LogError($"Failed to install package: {addRequest.Error.message}");

            EditorApplication.update -= InstallPackage;
        }
    }
}
