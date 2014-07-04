using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using Telerik.Sitefinity;
using Telerik.Sitefinity.Abstractions;
using Telerik.Sitefinity.Configuration;
using Telerik.Sitefinity.GenericContent.Model;
using Telerik.Sitefinity.Modules.GenericContent.Web.UI;
using Telerik.Sitefinity.Modules.Libraries;
using Telerik.Sitefinity.Modules.Pages;
using Telerik.Sitefinity.Modules.Pages.Configuration;
using Telerik.Sitefinity.Pages.Model;
using Telerik.Sitefinity.Utilities.Zip;
using Telerik.Sitefinity.Web.Configuration;
using Telerik.Sitefinity.Web.UI;
using Telerik.Sitefinity.Web.UI.NavigationControls;
using Telerik.Sitefinity.Web.UI.PublicControls;

namespace TemplateImporter
{
    public class TemplateImporter
    {
        private string sitefinityTemplatesInstallationFolder;
        private string templateExtractionFolder;
        private string zipFileName;
        private string applicationPath;

        private PageTemplate pageTemplate;
        private PageManager pageManager;
        private Template templateobject;

        /// <summary>
        /// Initializes a new instance of the <see cref="TemplateImporter" /> class.
        /// </summary>
        /// <param name="zipFileName">Name of the zip file.</param>
        /// <param name="applicationPath">The application path.</param>
        public TemplateImporter(string zipFileName, string applicationPath)
        {
            string uniqueExtension = DateTime.Now.ToFileTime().ToString();

            this.applicationPath = applicationPath;
            this.zipFileName = zipFileName;

            this.templateExtractionFolder = string.Concat(applicationPath, "App_Data\\temp_", uniqueExtension, "\\");
            this.sitefinityTemplatesInstallationFolder = string.Concat(applicationPath, "App_Data\\Sitefinity\\WebsiteTemplates\\");

            this.pageManager = PageManager.GetManager();
            this.pageTemplate = this.pageManager.CreateTemplate();
            this.pageTemplate.Category = new Guid();
        }


        /// <summary>
        /// Imports the uploaded template in the SF backend and registers it.
        /// </summary>
        public bool Import()
        {
            bool success = true;
            string fileToExtract = string.Concat(this.applicationPath, this.zipFileName);

            try
            {
                this.Extract(fileToExtract, this.templateExtractionFolder);

                this.GetTemplateFromXML();

                if (this.templateobject != null)
                {
                    //set template title
                    if (this.templateobject.Metadata == null || this.templateobject.Metadata.MetadataItems == null)
                    {
                        this.pageTemplate.Name = "untitled";
                        this.pageTemplate.Title = "untitled";
                    }
                    else
                    {
                        string title = this.templateobject.Metadata.MetadataItems.Where(m => m.Id == "title").First().Value;

                        this.pageTemplate.Name = title;
                        this.pageTemplate.Title = title;
                    }

                    string templateInstallationFolder = string.Concat(this.sitefinityTemplatesInstallationFolder, this.pageTemplate.Name);
                    this.CreateTemplateFolderStructure(templateInstallationFolder, this.pageTemplate.Name);


                    string cssPath = string.Concat(this.templateExtractionFolder, "css\\");
                    string imageSource = string.Concat(this.templateExtractionFolder, "images\\");
                    string masterPagePath = string.Concat(this.templateExtractionFolder, "page.master");

                    string themeTargetFolder = string.Concat(templateInstallationFolder, "\\App_Themes\\", this.pageTemplate.Name);
                    string imagesTargetFolder = string.Concat(themeTargetFolder, "\\Images");
                    string cssTargetFolder = string.Concat(themeTargetFolder, "\\Global\\");

                    if (File.Exists(masterPagePath))
                    {
                        File.Copy(masterPagePath, string.Concat(templateInstallationFolder, "\\App_Master\\page.master"), true);
                    }

                    if (Directory.Exists(imageSource))
                        this.CopyFiles(imageSource, imagesTargetFolder);

                    if (Directory.Exists(cssPath))
                    {
                        this.CopyFiles(cssPath, cssTargetFolder);
                        this.RegisterTheme();
                    }

                    UploadImages(imagesTargetFolder, this.pageTemplate.Name);

                    if (this.templateobject.Layout != null)
                    {
                        this.RegisterTemplate();
                    }
                }
                else
                {
                    success = false;
                }
            }
            catch (Exception ex)
            {
                success = false;
            }
            finally
            {
                File.Delete(fileToExtract);
                this.DeleteTemporaryFolder(this.templateExtractionFolder);

            }
            return success;
        }

        /// <summary>
        /// Deserializes the XML layout as a template object.
        /// </summary>
        private void GetTemplateFromXML()
        {
            string layoutFilePath = string.Concat(this.templateExtractionFolder, "layout.xml");

            XmlSerializer serializer = new XmlSerializer(typeof(Template));
            StreamReader reader = new StreamReader(layoutFilePath);

            try
            {
                object obj = serializer.Deserialize(reader);
                this.templateobject = (Template)obj;
                reader.Close();
            }
            catch (Exception ex)
            {

            }
        }


        /// <summary>
        /// Extracts the specified zip file name to the specified directory.
        /// </summary>
        /// <param name="zipFileArchive">Name of the zip file.</param>
        /// <param name="directory">The directory.</param>
        private void Extract(string zipFileArchive, string directory)
        {
            try
            {
                Directory.CreateDirectory(directory);
                using (ZipFile zip = ZipFile.Read(zipFileArchive))
                {
                    foreach (var file in zip.Entries)
                    {
                        file.Extract(directory, true);
                    }

                }
            }
            catch (System.Exception ex)
            {
            }
        }


        /// <summary>
        /// Creates the template folder structure.
        /// </summary>
        private void CreateTemplateFolderStructure(string templateRootDirecotry, string templateName)
        {
            Directory.CreateDirectory(templateRootDirecotry);
            Directory.CreateDirectory(string.Concat(templateRootDirecotry, "\\App_Master"));
            Directory.CreateDirectory(string.Concat(templateRootDirecotry, "\\App_Themes\\", templateName, "\\Global"));
            Directory.CreateDirectory(string.Concat(templateRootDirecotry, "\\App_Themes\\", templateName, "\\Images"));
        }



        /// <summary>
        /// Copies files
        /// </summary>
        /// <param name="sourcePath">The source path.</param>
        /// <param name="targetPath">The target path.</param>
        private void CopyFiles(string sourcePath, string targetPath)
        {
            string[] files = Directory.GetFiles(sourcePath);

            // Copy the files and overwrite destination files if they already exist.
            foreach (string s in files)
            {
                // Use static Path methods to extract only the file name from the path.
                string fileName = System.IO.Path.GetFileName(s);
                string destFile = System.IO.Path.Combine(targetPath, fileName);
                System.IO.File.Copy(s, destFile, true);
            }
        }


        /// <summary>
        /// Registers the theme.
        /// </summary>
        private void RegisterTheme()
        {
            this.pageTemplate.Theme = this.pageTemplate.Name;

            ConfigManager manager = Config.GetManager();
            var appearanceConfig = manager.GetSection<AppearanceConfig>();
            var defaultSamplesTheme = new ThemeElement(appearanceConfig.FrontendThemes)
            {
                Name = this.pageTemplate.Theme,
                Path = string.Concat("~/App_Data/Sitefinity/WebsiteTemplates/", this.pageTemplate.Name, "/App_Themes/", this.pageTemplate.Name)
            };

            if (!appearanceConfig.FrontendThemes.ContainsKey(defaultSamplesTheme.Name))
                appearanceConfig.FrontendThemes.Add(defaultSamplesTheme);

            appearanceConfig.DefaultFrontendTheme = this.pageTemplate.Theme;

            manager.SaveSection(appearanceConfig);
        }

        /// <summary>
        /// Registers the template.
        /// </summary>
        private void RegisterTemplate()
        {
            var present = this.pageManager.CreatePresentationItem<TemplatePresentation>();
            present.DataType = Presentation.HtmlDocument;
            present.Name = "master";
            var resName = "Telerik.Sitefinity.Resources.Pages.Frontend.aspx";
            present.Data = ControlUtilities.GetTextResource(resName, Config.Get<ControlsConfig>().ResourcesAssemblyInfo);

            this.pageTemplate.MasterPage = string.Concat("~/App_Data/Sitefinity/WebsiteTemplates/", this.pageTemplate.Name, "/App_Master/page.master");

            for (int i = 0; i < this.templateobject.Layout.Placeholders.Length; i++)
            {
                var placeholder = this.templateobject.Layout.Placeholders[i];

                for (int j = 0; j < placeholder.LayoutWidget.Columns.Length; j++)
                {
                    var column = placeholder.LayoutWidget.Columns[j];

                    var widget = column.Widget;
                    if (widget.Type != null)
                    {
                        ControlData ctrlData = null;
                        if (widget.Type.ToLower() == "content block")
                        {
                            ContentBlockBase newContentBlock = new ContentBlockBase();
                            newContentBlock.Html = widget.Properties.Text;
                            newContentBlock.CssClass = widget.CssClass;
                            newContentBlock.LayoutTemplatePath = "~/SFRes/Telerik.Sitefinity.Resources.Templates.Backend.GenericContent.ContentBlock.ascx";

                            var templateContentBlock = this.pageManager.CreateControl<Telerik.Sitefinity.Pages.Model.TemplateControl>(newContentBlock, widget.SfID);
                            templateContentBlock.Caption = "Content Block";

                            this.pageTemplate.Controls.Add(templateContentBlock);
                            ctrlData = templateContentBlock;
                        }
                        else if (widget.Type.ToLower() == "image")
                        {
                            ImageControl newImage = new ImageControl();
                            newImage.LayoutTemplatePath = "~/SFRes/Telerik.Sitefinity.Resources.Templates.PublicControls.ImageControl.ascx";
                            newImage.CssClass = widget.CssClass;
                            newImage.ImageId = GetImageId(widget.Properties.Filename, this.pageTemplate.Name);

                            var templateImageControl = this.pageManager.CreateControl<Telerik.Sitefinity.Pages.Model.TemplateControl>(newImage, widget.SfID);
                            templateImageControl.Caption = "Image";

                            this.pageTemplate.Controls.Add(templateImageControl);
                            ctrlData = templateImageControl;

                        }
                        else if (widget.Type.ToLower() == "navigation")
                        {
                            string type = widget.Properties.NavigationType;
                            NavigationControl navigation = new NavigationControl();

                            navigation.SelectionMode = PageSelectionModes.TopLevelPages;
                            NavigationModes navigationMode;

                            switch (type)
                            {
                                case "horizontalcontrol": navigationMode = NavigationModes.HorizontalSimple;
                                    break;

                                case "horizontal2levelscontrol": navigationMode = NavigationModes.HorizontalDropDownMenu;
                                    break;

                                case "tabscontrol": navigationMode = NavigationModes.HorizontalTabs;
                                    break;

                                case "verticalcontrol": navigationMode = NavigationModes.VerticalSimple;
                                    break;

                                case "treeviewcontrol": navigationMode = NavigationModes.VerticalTree;
                                    break;

                                case "sitemapcontrol": navigationMode = NavigationModes.SiteMapInColumns;
                                    break;

                                default: navigationMode = NavigationModes.HorizontalSimple;
                                    break;
                            }

                            navigation.NavigationMode = navigationMode;
                            navigation.Skin = widget.CssClass;

                            var templateNavigationControl = this.pageManager.CreateControl<Telerik.Sitefinity.Pages.Model.TemplateControl>(navigation, widget.SfID);
                            templateNavigationControl.Caption = "Navigation";

                            this.pageTemplate.Controls.Add(templateNavigationControl);
                            ctrlData = templateNavigationControl;

                        }

                        var widgetCulture = this.GetCurrentLanguage();
                        this.pageManager.SetControlId(this.pageTemplate, ctrlData, widgetCulture);
                    }
                }
            }

            this.pageTemplate.Category = Telerik.Sitefinity.Abstractions.SiteInitializer.CustomTemplatesCategoryId;
            this.pageManager.SaveChanges();

            // publish the template
            var draft = this.pageManager.EditTemplate(this.pageTemplate.Id);
            var master = this.pageManager.TemplatesLifecycle.CheckOut(draft);
            master = this.pageManager.TemplatesLifecycle.CheckIn(master);
            this.pageManager.TemplatesLifecycle.Publish(master);
            this.pageManager.SaveChanges();

        }

        protected CultureInfo GetCurrentLanguage()
        {
            var enableWidgetTranslations = Config.Get<PagesConfig>().EnableWidgetTranslations;
            var widgetCulture = CultureInfo.CurrentUICulture;
            if (enableWidgetTranslations.HasValue == false || enableWidgetTranslations.Value == false || AppSettings.CurrentSettings.Multilingual == false)
            {
                widgetCulture = null;
            }

            return widgetCulture;
        }

        /// <summary>
        /// Deletes the temporary folder.
        /// </summary>
        /// <param name="folderName">Name of the folder.</param>
        private void DeleteTemporaryFolder(string folderName)
        {
            try
            {
                DirectoryInfo dir = new DirectoryInfo(folderName);
                dir.Delete(true);
            }
            catch (Exception ex)
            {
            }
        }

        public static void UploadImages(string folderPath, string albumName)
        {
            var imagesCreated = App.WorkWith().Albums().Where(i => i.Title == albumName).Get().Count() > 0;
            if (!imagesCreated)
            {
                DirectoryInfo myFolder = new DirectoryInfo(folderPath);
                var album = App.WorkWith().Album().CreateNew().Do(a => { a.Title = albumName; a.Description = "Images imported from template"; }).SaveAndContinue();

                foreach (var file in myFolder.GetFiles())
                {
                    album.CreateImage()
                       .Do(image1 => { image1.Title = file.Name; image1.Description = file.Name; image1.UrlName = file.Name; })
                       .CheckOut().UploadContent(file.OpenRead(), file.Extension)
                       .CheckInAndPublish()
                       .SaveChanges();
                }
            }
        }

        public static Guid GetImageId(string imageName, string albumTitle)
        {
            var librariesManager = LibrariesManager.GetManager();
            Guid imageId = Guid.Empty;


            var album = librariesManager.GetAlbums().Where(a => a.Title.Equals(albumTitle)).FirstOrDefault();
            if (album == null)
                return imageId;
            var s = Path.GetFileName(imageName);
            var image = album.Images().Where(i => i.Title == Path.GetFileName(imageName) && i.Status == ContentLifecycleStatus.Live).FirstOrDefault();

            if (image != null)
            {
                imageId = image.Id;
            }

            return imageId;
        }

    }
}