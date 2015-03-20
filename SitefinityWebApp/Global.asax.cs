using System;
using Telerik.Sitefinity.Abstractions;
using Telerik.Sitefinity.Data;
using Telerik.Sitefinity.Modules.GenericContent.Web.UI;
using Telerik.Sitefinity.Samples.Common;
using Telerik.Sitefinity.Services;
using TemplateImporter;

namespace SitefinityWebApp
{
    public class Global : System.Web.HttpApplication
    {
        private const string SamplesThemeName = "SamplesTheme";
        private const string SamplesThemePath = "~/App_Data/Sitefinity/WebsiteTemplates/Samples/App_Themes/Samples";

        private const string SamplesTemplateId = "015b4db0-1d4f-4938-afec-5da59749e0e8";
        private const string SamplesTemplateName = "SamplesMasterPage";
        private const string SamplesTemplatePath = "~/App_Data/Sitefinity/WebsiteTemplates/Samples/App_Master/Samples.master";

        private const string HomePageId = "2FF33642-8F5C-49DA-8FA8-BCA5307DC846";
        private const string HomePageName = "TemplateImporterSample";

        protected void Application_Start(object sender, EventArgs e)
        {
            Bootstrapper.Initialized += Bootstrapper_Initialized;
        }

        void Bootstrapper_Initialized(object sender, ExecutedEventArgs e)
        {
            if (e.CommandName == "RegisterRoutes")
            {
                SampleUtilities.RegisterModule<TemplateImporterModule>("Template Importer", "This module imports templates from template builder.");
            }
            if ((Bootstrapper.IsDataInitialized) && (e.CommandName == "Bootstrapped"))
            {
                SystemManager.RunWithElevatedPrivilegeDelegate worker = new SystemManager.RunWithElevatedPrivilegeDelegate(CreateSampleWorker);
                SystemManager.RunWithElevatedPrivilege(worker);
            }
        }

        private void CreateSampleWorker(object[] args)
        {
            SampleUtilities.RegisterTheme(SamplesThemeName, SamplesThemePath);
            SampleUtilities.RegisterTemplate(new Guid(SamplesTemplateId), SamplesTemplateName, SamplesTemplateName, SamplesTemplatePath, SamplesThemeName);

            var result = SampleUtilities.CreatePage(new Guid(HomePageId), HomePageName, true);

            if (result)
            {
                SampleUtilities.SetTemplateToPage(new Guid(HomePageId), new Guid(SamplesTemplateId));

                // add a note with a link to the Template Builder
                ContentBlockBase contentBlock = new ContentBlockBase();
                contentBlock.Html = @"<h2>Note:</h2><p>The module in this sample works with templates, created using the <a href='http://www.sitefinity.com/devnet/templatebuilder.aspx'>Sitefinity Template Builder</a></p>";
                SampleUtilities.AddControlToPage(new Guid(HomePageId), contentBlock, "Content", "Content Block");
            }    
        }

        protected void Session_Start(object sender, EventArgs e)
        {

        }

        protected void Application_BeginRequest(object sender, EventArgs e)
        {

        }

        protected void Application_AuthenticateRequest(object sender, EventArgs e)
        {

        }

        protected void Application_Error(object sender, EventArgs e)
        {

        }

        protected void Session_End(object sender, EventArgs e)
        {

        }

        protected void Application_End(object sender, EventArgs e)
        {

        }
    }
}