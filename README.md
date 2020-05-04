This provides a really simple continuous deployment mechanism for WinForms apps, building and deploying an installer package to Azure blob storage. It also provides a complementary Nuget package **AO.AzDeploy.Client** used to enable applications to check for updated installers, and download and launch them if available. This is a reboot of my [AzureDeploy](https://github.com/adamosoftware/AzureDeploy) project to simplify and refactor some dependencies.

I'm using [DeployMaster](https://www.deploymaster.com/index.html) as my packaging app, and regular Azure blob storage as my hosting source. There's no dependency on DeployMaster, but you do need some kind of installer app that can build a package from a command line that accepts a version number parameter.

To implement this, first create a json file based on this [DeployScript](https://github.com/adamosoftware/AzDeploy/blob/master/AzDeploy.Build/Models/DeployScript.cs) model. Example:

![img](https://adamosoftware.blob.core.windows.net:443/images/AzDeployScript.png)

The `versionSourceFile` specifies the file in your project that defines the overall version number of your project. You'll increment the version of this file to release new versions of your app. Note also the `installerArgs` value uses a special value `%version%` to indicate where the app version number is injected when the command is executed.

Next, add a post-build event to your WinForms project that calls **AzDeploy.Build.exe** and uses the json file you created above as an argument. Example:

![img](https://adamosoftware.blob.core.windows.net:443/images/AzDeployPostBuild.png)

Note, that you'll need to clone this repo and build `AzDeploy.Build.exe` yourself -- I haven't added any binaries to this repo. But once you get this much setup, incrementing your app version and building the project will trigger an install build and upload the output to blob storage. Here's where this happens [internally](https://github.com/adamosoftware/AzDeploy/blob/master/AzDeploy.Build/Program.cs#L14).

Now, it's time to set up your application to look for new versions. To do this:

- Install Nuget package **AO.AzDeploy.Client** in your application.
- Create a class based on abstract class [InstallHelper](https://github.com/adamosoftware/AzDeploy/blob/master/AzDeploy.Client/InstallHelper.cs). This is abstract because I wanted to avoid adding explicit WinForms dependencies, so you must implement some methods. Here's a sample implementation I'm using:

```csharp
internal class AppInstallHelper : InstallHelper
{
	public AppInstallHelper() : base(Version.Parse(Application.ProductVersion), "your account", "your container", "ModelSyncSetup.exe")
	{
	}

	protected override void ExitApplication()
	{
		Application.Exit();
	}

	protected override bool PromptDownloadAndExit()
	{
		return (MessageBox.Show(
			"A new version is available. Click OK to download and exit the application now.", 
			"New Version Available", MessageBoxButtons.OKCancel) == DialogResult.OK);
	}
}
```
Lastly, add an `AppInstallHelper` instance somewhere like an About Box in your application. Example:

![img](https://adamosoftware.blob.core.windows.net:443/images/AzDeployAboutBox.png)

Here's the code behind:

```csharp
public partial class frmAbout : Form
{
	AppInstallHelper _installer = new AppInstallHelper();

	public frmAbout()
	{
		InitializeComponent();
	}

	private async void frmAbout_Load(object sender, EventArgs e)
	{
		lblVersion.Text = Application.ProductVersion;

		var check = await _installer.GetVersionCheckAsync();
		panel1.Visible = check.IsNew;
		lblNewVersion.Text = $"Version {check.Version} available:";
	}

	private void button1_Click(object sender, EventArgs e)
	{
		_installer.AutoInstallAsync();
	}
}
```
