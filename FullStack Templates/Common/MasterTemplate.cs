﻿using System;
using System.IO;
using System.Data;
using System.Data.SqlClient;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;
using System.Xml;
using System.Reflection;
using System.Collections.Generic;
using CodeSmith.Engine;
using SchemaExplorer;

namespace FullStack.Common
{
    public class MasterTemplate : CodeTemplate
    {
        #region Constants

        public const int NUM_OF_COLUMNS = 3;

        #endregion

        #region Fields

        public MapCollection DbDataReader = Map.LoadFromName("DbType-DataReaderMethod"); //new MapCollection("../../Maps/System-CSharpAlias.csmap");

        public MapCollection DbTypeCSharp = Map.LoadFromName("DbType-CSharp"); //new MapCollection("../../Maps/System-CSharpAlias.csmap");

        public MapCollection SqlCSharp = Map.LoadFromName("Sql-CSharp"); //new MapCollection("../../Maps/System-CSharpAlias.csmap");

        public MapCollection SqlNativeSqlDb = Map.LoadFromName("SqlNativeType-SqlDbType"); //new MapCollection("../../Maps/System-CSharpAlias.csmap");

        private string _outputDirectory = String.Empty;

        #endregion

        #region Constructors and Destructors

        public MasterTemplate()
            : base()
        {
        }

        #endregion

        #region Public Properties

        public string ApiClassAlias
        {
            get
            {
                return string.Format("{0}Api", this.SolutionName);
            }
        }
        
        [Category("Context")]
        [Description("Url to use when assigning claims, such as 'MySite.com' or 'YourSite.com'")]
        public string BaseUrl { get; set; }

        [Category("Context")]
        public string CompanyName { get; set; }

        [Category("Context")]
        [DefaultValue(true)]
        [Description("If set to true then the .sln and .csproj files will be created")]
        public bool CreateProjectFiles { get; set; }

        [Category("InProcess")]
        public string CurrentProjectAlias
        {
            get
            {
                if (String.IsNullOrEmpty(this.ProjectName))
                {
                    return "TotallyUnknown";
                }

                if (this.StackProjects.Keys.Contains(this.ProjectName))
                {
                    return this.StackProjects[this.ProjectName].Alias;
                }
                throw new Exception(string.Format("Unknown key: [{0}]", this.ProjectName));
            }
        }

        [Optional]
        [Category("Context")]
        public string ExceptionlessApiKey { get; set; }

        [Optional]
        [Category("Context")]
        public SchemaExplorer.TableSchema CurrentTable { get; set; }

        [Editor(typeof(System.Windows.Forms.Design.FolderNameEditor), typeof(System.Drawing.Design.UITypeEditor))]
        [Optional]
        [Category("Output")]
        [Description("The directory to output the results to.")]
        public string OutputDirectory
        {
            get
            {
                // default to the directory that the template is located in
                if (this._outputDirectory.Length == 0)
                {
                    return Path.Combine(CodeTemplateInfo.DirectoryName, "output\\");
                }

                return this._outputDirectory;
            }
            set
            {
                if (!value.EndsWith("\\"))
                {
                    value += "\\";
                }
                this._outputDirectory = value;
            }
        }
        
         [Editor(typeof(System.Windows.Forms.Design.FolderNameEditor), typeof(System.Drawing.Design.UITypeEditor))]
        [Optional]
        [Category("Context")]
        [Description("If this is populated, these files will be copied to the resource/database/v1 folder of the engine project. The names of the files should match the existing database files")]
        public string DatabaseSetupFilesDirectory { get; set; }
        
        [Optional]
        [Category("InProcess")]
        public string ProjectName { get; set; }

        [Optional]
        [Category("InProcess")]
        public bool RenderBody { get; set; }

        [Category("Context")]
        public string SolutionName { get; set; }

        [Category("Context")]
        public SchemaExplorer.DatabaseSchema SourceDatabase { get; set; }

        [Optional]
        [Category("InProcess")]
        public Dictionary<String, ProjectInfo> StackProjects { get; set; }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        ///     Adds the given file to the indicated project
        /// </summary>
        /// <param name="project">The path of the proj file</param>
        /// <param name="projectSubDir">
        ///     The subdirectory of the project that the
        ///     file is located in, otherwise an empty string if it is at the project root
        /// </param>
        /// <param name="file">The name of the file to be added to the project</param>
        /// <param name="parent">
        ///     The name of the parent to group the file to, an
        ///     empty string if there is no parent file
        /// </param>
        public void AddAssemblyToProject(string projectPath, string assembly)
        {
            this.AddAssemblyToProject(projectPath, assembly, string.Empty);   
        }
        
        /// <summary>
        ///     Adds the given file to the indicated project
        /// </summary>
        /// <param name="project">The path of the proj file</param>
        /// <param name="projectSubDir">
        ///     The subdirectory of the project that the
        ///     file is located in, otherwise an empty string if it is at the project root
        /// </param>
        /// <param name="file">The name of the file to be added to the project</param>
        /// <param name="parent">
        ///     The name of the parent to group the file to, an
        ///     empty string if there is no parent file
        /// </param>
        public void AddAssemblyToProject(string projectPath, string assembly, string hintPath)
        {
            XDocument proj = XDocument.Load(projectPath);

            XNamespace ns = "http://schemas.microsoft.com/developer/msbuild/2003";
            XElement itemGroup = proj.Descendants(ns + "ItemGroup").FirstOrDefault(x => x.Descendants(ns + "Reference").Count() > 0);

            if (itemGroup == null)
            {
                throw new Exception(string.Format("Unable to find an ItemGroup to add the assembly {1} to the {0} project", projectPath, assembly));
            }

            //If the file is already listed, don't bother adding it again
            if (itemGroup.Descendants(ns + "Reference").Where(x => x.Attribute("Include").Value.ToString() == assembly).Count() > 0)
            {
                return;
            }

            var item = new XElement(ns + "Reference", new XAttribute("Include", assembly));
            if (!string.IsNullOrEmpty(hintPath))
            {
                var hint = new XElement(ns + "HintPath", hintPath);               
                item.Add(hint);
            }
            itemGroup.Add(item);

            proj.Save(projectPath);
        }

        /// <summary>
        ///     Adds the given file to the indicated project
        /// </summary>
        /// <param name="project">The path of the proj file</param>
        /// <param name="projectSubDir">
        ///     The subdirectory of the project that the
        ///     file is located in, otherwise an empty string if it is at the project root
        /// </param>
        /// <param name="file">The name of the file to be added to the project</param>
        /// <param name="parent">
        ///     The name of the parent to group the file to, an
        ///     empty string if there is no parent file
        /// </param>
        public void AddFileToProject(string projectPath, string projectSubDir, string file, string parent)
        {
             string modifier = "Compile";
            if (file.EndsWith(".config", StringComparison.InvariantCultureIgnoreCase) || file.EndsWith(".sql", StringComparison.InvariantCultureIgnoreCase) || file.EndsWith(".xml", StringComparison.InvariantCultureIgnoreCase) || file.EndsWith(".txt", StringComparison.InvariantCultureIgnoreCase) || file.EndsWith(".cshtml", StringComparison.InvariantCultureIgnoreCase)){
                modifier = "None";
                if (projectSubDir.StartsWith(@"Resources", StringComparison.InvariantCultureIgnoreCase))
                {
                    modifier = "EmbeddedResource";
                } 
                else if (file.EndsWith(".cshtml", StringComparison.InvariantCultureIgnoreCase))
                {
                    modifier = "Content";    
                }
            }
            
            XDocument proj = XDocument.Load(projectPath);

            XNamespace ns = "http://schemas.microsoft.com/developer/msbuild/2003";
            XElement itemGroup = proj.Descendants(ns + "ItemGroup").FirstOrDefault(x => x.Descendants(ns + modifier).Count() > 0);

            if (itemGroup == null)
            {
                throw new Exception(string.Format("Unable to find an ItemGroup to add the file {1} to the {0} project", projectPath, file));
            }

           
            //If the file is already listed, don't bother adding it again
            if (itemGroup.Descendants(ns + modifier).Where(x => x.Attribute("Include").Value.ToString() == Path.Combine(projectSubDir, file)).Count() > 0)
            {
                return;
            }
            
            
            var item = new XElement(ns + modifier, new XAttribute("Include", Path.Combine(projectSubDir, file)));

            //This is used to group files together, in this case the file that is 
            //regenerated is grouped as a dependent of the user-editable file that
            //is not changed by the code generator
            if (string.IsNullOrEmpty(parent) == false)
            {
                item.Add(new XElement(ns + "DependentUpon", parent));
            }

            itemGroup.Add(item);

            proj.Save(projectPath);
        }

        /// <summary>
        ///     Adds the given file to the indicated project
        /// </summary>
        /// <param name="project">The path of the proj file</param>
        /// <param name="projectSubDir">
        ///     The subdirectory of the project that the
        ///     file is located in, otherwise an empty string if it is at the project root
        /// </param>
        /// <param name="file">The name of the file to be added to the project</param>
        /// <param name="parent">
        ///     The name of the parent to group the file to, an
        ///     empty string if there is no parent file
        /// </param>
        public void AddProjectReferenceToProject(string projectPath, string projectToReferenceRelativePath, Guid projectToReferenceGuid, string projectToReferenceName)
        {
            XDocument proj = XDocument.Load(projectPath);

            XNamespace ns = "http://schemas.microsoft.com/developer/msbuild/2003";
            XElement itemGroup = proj.Descendants(ns + "ItemGroup").FirstOrDefault(x => x.Descendants(ns + "ProjectReference").Count() > 0);

            if (itemGroup == null)
            {
                throw new Exception(string.Format("Unable to find an ItemGroup to add the project reference {1} to the {0} project", projectPath, projectToReferenceName));
            }

            //If the file is already listed, don't bother adding it again
            if (itemGroup.Descendants(ns + "ProjectReference").Where(x => x.Attribute("Include").Value.ToString() == projectToReferenceRelativePath).Count() > 0)
            {
                return;
            }

            var item = new XElement(ns + "ProjectReference", new XAttribute("Include", projectToReferenceRelativePath));
            item.Add(new XElement(ns + "Project", projectToReferenceGuid.ToString("B")));
            item.Add(new XElement(ns + "Name", projectToReferenceName));

            itemGroup.Add(item);

            proj.Save(projectPath);
        }

        public void BuildCurrentTableTemplate(CodeTemplate template, string projectLocation, string subFolderPath, string generatedFileNameFormat, string parentFileNameFormat)
        {
            if (!string.IsNullOrEmpty(parentFileNameFormat))
            {
                this.ExecuteTemplate(template, Path.Combine(this.OutputDirectory, this.SolutionName, this.CurrentProjectAlias, subFolderPath, parentFileNameFormat));
                this.AddFileToProject(projectLocation, subFolderPath, string.Format(parentFileNameFormat, this.GetClassName(this.CurrentTable)), string.Empty);

                this.ExecuteTemplate(template, Path.Combine(this.OutputDirectory, this.SolutionName, this.CurrentProjectAlias, subFolderPath, generatedFileNameFormat));
                this.AddFileToProject(projectLocation, subFolderPath, string.Format(generatedFileNameFormat, this.GetClassName(this.CurrentTable)), string.Format(parentFileNameFormat, this.GetClassName(this.CurrentTable)));
            }
            else
            {
                this.ExecuteTemplate(template, Path.Combine(this.OutputDirectory, this.SolutionName, this.CurrentProjectAlias, subFolderPath, generatedFileNameFormat));
                this.AddFileToProject(projectLocation, subFolderPath, string.Format(generatedFileNameFormat, this.GetClassName(this.CurrentTable)), string.Empty);
            }
        }

        public void CleanupProjectFile(string projectPath)
        {
            XDocument proj = XDocument.Load(projectPath);
            bool projectUpdated = false;
            //If the file is already listed, don't bother adding it again
            string[] modifiers = new []{"ProjectReference", "EmbeddedResource"};
            
            foreach (var modifier in modifiers)
        	{
                  XNamespace ns = "http://schemas.microsoft.com/developer/msbuild/2003";
                  XElement itemGroup = proj.Descendants(ns + "ItemGroup").FirstOrDefault(x => x.Descendants(ns + modifier).Count() > 0);

                if (itemGroup != null)
                {
                    IEnumerable<XElement> foundElements = itemGroup.Descendants(ns + modifier).Where(x => x.Attribute("Include").Value.ToString() == string.Empty);
                    IList<XElement> xElements = foundElements as IList<XElement> ?? foundElements.ToList();
                    if (xElements.Any())
                    {
                        foreach (XElement foundElement in xElements)
                        {
                            foundElement.Remove();
                            projectUpdated = true;
                        }
                    }
                }
        		
        	}
            
            if (projectUpdated)
            {
                proj.Save(projectPath);    
            }
            
        }

        /// <summary>
        ///     Clears out a directory
        /// </summary>
        /// <param name="path"></param>
        public void ClearDirectory(string path)
        {
            if (!Directory.Exists(path))
            {
                // Create output directory.
                Directory.CreateDirectory(path);
            }
            else
            {
                if (this.CreateProjectFiles)
                {
                    this.DeleteFiles(path, "*.sln");
                    this.DeleteFiles(path, "*.csproj");
                    this.DeleteFiles(path, "*.config");
                    this.DeleteFiles(path, "*.exe");
                    this.DeleteFiles(path, "*.targets");
                }
                // Clean up the existing output directory.
                this.DeleteFiles(path, "*.cs");
            }
        }

        public void CopyFileToProject(string projectLocation, string relativeSourceFile, string outputSubDir, string outputFileName)
        {
            this.CopyFileToProject(projectLocation, relativeSourceFile, outputSubDir, outputFileName, string.Empty);
        }

        public void CopyFileToProject(string projectLocation, string relativeSourceFile, string outputSubDir, string outputFileName, string parentFile)
        {
            if (!Directory.Exists(Path.Combine(this.OutputDirectory, this.SolutionName, this.CurrentProjectAlias, outputSubDir)))
            {
                Directory.CreateDirectory(Path.Combine(this.OutputDirectory, this.SolutionName, this.CurrentProjectAlias, outputSubDir));
            }

            File.Copy(Path.Combine(this.CodeTemplateInfo.DirectoryName, relativeSourceFile), Path.Combine(this.OutputDirectory, this.SolutionName, this.CurrentProjectAlias, outputSubDir, outputFileName));
            this.AddFileToProject(projectLocation, outputSubDir, outputFileName, parentFile);
        }

        public void CopyNonTemplateFilesToFolder(string relativeSourceDirectory, string relativeTargetDirectory)
        {
            foreach (string rootFile in Directory.EnumerateFiles(Path.Combine(this.CodeTemplateInfo.DirectoryName, relativeSourceDirectory)))
            {
                var fi = new FileInfo(rootFile);
                if (!fi.Name.EndsWith(".cst"))
                {
                    File.Copy(fi.FullName, this.GetProjectOutputDirectory(relativeTargetDirectory, fi.Name));
                }
            }
        }

        public void DeleteFiles(string directory, string searchPattern)
        {
            string[] files = Directory.GetFiles(directory, searchPattern);

            for (int i = 0; i < files.Length; i++)
            {
                try
                {
                    File.Delete(files[i]);
                }
                catch (Exception ex)
                {
                    Response.WriteLine("Error while attempting to delete file (" + files[i] + ").\r\n" + ex.Message);
                }
            }
        }

        public void ExecuteTemplate(CodeTemplate template, string formatPath)
        {
            this.CopyPropertiesTo(template);
            var masterTemplate = template as MasterTemplate;
            if (masterTemplate != null)
            {
                masterTemplate.RenderBody = formatPath.Contains(".gen.");
            }

            if (formatPath.Contains("{0}"))
            {
                template.RenderToFile(string.Format(formatPath, this.GetClassName(this.CurrentTable)), true);
            }
            else
            {
                template.RenderToFile(formatPath, true);
            }
        }

        public string GetCSharpVariableType(ColumnSchema column)
        {
            if (column.Name.EndsWith("TypeCode"))
            {
                return column.Name;
            }

            return this.DbTypeCSharp[column.DataType.ToString()];
        }

        public string GetCamelCaseName(string value)
        {
            return value.Substring(0, 1).ToLower() + value.Substring(1);
        }

        public string GetClassName(TableSchema table)
        {
            if (table.ExtendedProperties.Contains("SingularName"))
            {
                return (string)table.ExtendedProperties["SingularName"].Value;
            }
            else
            {
                if (table.Name.EndsWith("s"))
                {
                    return table.Name.Substring(0, table.Name.Length - 1);
                }
                else
                {
                    return table.Name;
                }
            }
        }
        
         public string GetClassName(string className)
        {
            return className;
        }


        public string GetColumnSize(ColumnSchema column)
        {
            string columnSize = column.Size.ToString();

            if (column.NativeType == "numeric" && column.Precision != 0)
            {
                columnSize += "(" + column.Precision.ToString() + "," + column.Scale + ")";
            }

            return columnSize;
        }

        public string GetColumnSize(ViewColumnSchema column)
        {
            string columnSize = column.Size.ToString();

            if (column.NativeType == "numeric" && column.Precision != 0)
            {
                columnSize += "(" + column.Precision.ToString() + "," + column.Scale + ")";
            }

            return columnSize;
        }

        public string GetDataClassName(TableSchema table)
        {
            return string.Format("{0}Data", this.GetClassName(table));
        }
        
         public string GetDataClassName(string className)
        {
            return string.Format("{0}Data", className);
        }

        public string GetManagerClassName(TableSchema table)
        {
            return string.Format("{0}Manager", this.GetClassName(table));
        }

          public string GetManagerClassName(string className)
        {
            return string.Format("{0}Manager", className);
        }

        public string GetMemberVariableDeclarationStatement(ColumnSchema column)
        {
            return this.GetMemberVariableDeclarationStatement("private", column);
        }

        public string GetMemberVariableDeclarationStatement(string protectionLevel, ColumnSchema column)
        {
            string statement = protectionLevel + " ";
            statement += this.GetCSharpVariableType(column) + " " + this.GetMemberVariableName(column);

            string defaultValue = this.GetMemberVariableDefaultValue(column);
            if (defaultValue != "")
            {
                statement += " = " + defaultValue;
            }

            statement += ";";

            return statement;
        }

        public string GetMemberVariableDefaultValue(ColumnSchema column)
        {
            switch (column.DataType)
            {
                case DbType.Guid:
                    {
                        return "Guid.Empty";
                    }
                case DbType.AnsiString:
                case DbType.AnsiStringFixedLength:
                case DbType.String:
                case DbType.StringFixedLength:
                    {
                        return "String.Empty";
                    }
                default:
                    {
                        return "";
                    }
            }
        }

        public string GetMemberVariableName(ColumnSchema column)
        {
            string propertyName = this.GetPropertyName(column);
            string memberVariableName = "_" + this.GetCamelCaseName(propertyName);

            return memberVariableName;
        }

         public string GetModelClassName(string className)
        {
            return string.Format("{0}Model", className);
        }

        public string GetModelClassName(TableSchema table)
        {
            return string.Format("{0}Model", this.GetClassName(table));
        }

        public string GetParameterSize(ParameterSchema parameter)
        {
            string parameterSize = parameter.Size.ToString();

            if (parameter.NativeType == "numeric" && parameter.Precision != 0)
            {
                parameterSize += "(" + parameter.Precision.ToString() + "," + parameter.Scale + ")";
            }

            return parameterSize;
        }

        public string GetPrimaryKeyName(TableSchema table)
        {
            if (table.PrimaryKey != null)
            {
                if (table.PrimaryKey.MemberColumns.Count == 1)
                {
                    return this.GetPropertyName(table.PrimaryKey.MemberColumns[0]);
                }
                else
                {
                    string columns = string.Empty;
                    columns = String.Join(",", table.PrimaryKey.MemberColumns.Select(m => m.Name));
                    throw new ApplicationException(string.Format("This template will not work on the table {0} because it has primary keys with more than one member column: {1}", table.Name, columns));
                }
            }
            else
            {
                throw new ApplicationException(string.Format("Error parsing the table [{0}]. This template will only work on tables with a primary key.", table.Name));
            }
        }

        public string GetPrimaryKeyType(TableSchema table)
        {
            if (table.PrimaryKey != null)
            {
                if (table.PrimaryKey.MemberColumns.Count == 1)
                {
                    return this.GetCSharpVariableType(table.PrimaryKey.MemberColumns[0]);
                }
                else
                {
                    string columns = string.Empty;

                    columns = String.Join(",", table.PrimaryKey.MemberColumns.Select(m => m.Name));

                    throw new ApplicationException(string.Format("This template will not work on the table {0} because it has primary keys with more than one member column: {1}", table.Name, columns));
                }
            }
            else
            {
                throw new ApplicationException(string.Format("Error parsing the table [{0}]. This template will only work on tables with a primary key.", table.Name));
            }
        }
        
        public string GetPrimaryKeyToString(TableSchema table, string variableName)
        {
            string keyType = GetPrimaryKeyType(table);
            
            if (keyType.Equals("string", StringComparison.InvariantCultureIgnoreCase)){
                return variableName;    
            } 
            else 
            {
                return string.Format("{0}.ToString()", variableName);
            }
        }

        public string GetProjectOutputDirectory(string relativeTargetDirectory)
        {
            return Path.Combine(this.OutputDirectory, this.SolutionName, this.CurrentProjectAlias, relativeTargetDirectory);
        }

        public string GetProjectOutputDirectory(string relativeTargetDirectory, string fileName)
        {
            return Path.Combine(this.OutputDirectory, this.SolutionName, this.CurrentProjectAlias, relativeTargetDirectory, fileName);
        }

        public string GetProjectTypeGuid(ProjectType en)
        {
            Type type = en.GetType();

            MemberInfo[] memInfo = type.GetMember(en.ToString());

            if (memInfo != null && memInfo.Length > 0)
            {
                object[] attrs = memInfo[0].GetCustomAttributes(typeof(DescriptionAttribute), false);

                if (attrs != null && attrs.Length > 0)
                {
                    return ((DescriptionAttribute)attrs[0]).Description;
                }
            }

            return en.ToString();
        }

        public string GetPropertyName(ColumnSchema column)
        {
            string propertyName = column.Name;

            if (propertyName == column.Table.Name + "Name")
            {
                return "Name";
            }
            if (propertyName == column.Table.Name + "Description")
            {
                return "Description";
            }

            if (propertyName.EndsWith("TypeCode"))
            {
                propertyName = propertyName.Substring(0, propertyName.Length - 4);
            }

            return propertyName;
        }

        public string GetReaderAssignmentStatement(ColumnSchema column, int index)
        {
            string statement = "if (!reader.IsDBNull(" + index.ToString() + ")) ";
            statement += this.GetMemberVariableName(column) + " = ";

            if (column.Name.EndsWith("TypeCode"))
            {
                statement += "(" + column.Name + ")";
            }

            statement += "reader." + this.GetReaderMethod(column) + "(" + index.ToString() + ");";

            return statement;
        }

        public string GetReaderMethod(ColumnSchema column)
        {
            return this.DbDataReader[column.DataType.ToString()];
        }

        public string GetSqlDbType(ColumnSchema column)
        {
            return this.SqlNativeSqlDb[column.NativeType.ToString()];
        }

        public SqlDataReader GetSystemInformation(string connectionString)
        {
            var cn = new SqlConnection(connectionString);
            var cmd = new SqlCommand();

            cmd.Connection = cn;
            cmd.CommandText = "master.dbo.xp_msver";
            cmd.CommandType = CommandType.StoredProcedure;

            cn.Open();
            SqlDataReader reader = cmd.ExecuteReader(CommandBehavior.CloseConnection);

            return reader;
        }

        public bool HasValidPrimaryKey(TableSchema table)
        {
            bool result = false;
            if (table.PrimaryKey != null)
            {
                if (table.PrimaryKey.MemberColumns.Count == 1)
                {
                    result = true;
                }
            }
            return result;
        }

        public void OnProgress(object sender, ProgressEventArgs e)
        {
            //if (e.Value > 0)
            //{
            //    this.Progress.Value = 75 + (_currentStep * 100) + (int)(((Double)e.Value / (Double)e.MaximumValue) * 100);
            //}
        }

        public void OutputExceptionInformation(Exception exception, int indentLevel)
        {
            int originalIndentLevel = Response.IndentLevel;
            Response.IndentLevel = indentLevel;
            Response.WriteLine("<table width=\"95%\">");
            Response.WriteLine("<tr>");
            Response.WriteLine("	<td>");
            Response.WriteLine("		<span class=\"exceptionText\">");
            Response.WriteLine("		An exception occurred while attempting to execute the template:");
            Response.WriteLine("		" + exception.Message);
            Response.WriteLine("		</span>");
            Response.WriteLine("	</td>");
            Response.WriteLine("</tr>");
            Response.WriteLine("</table>");
            Response.IndentLevel = originalIndentLevel;
        }

        public void OutputSystemInformation(string connectionString, int indentLevel)
        {
            SqlDataReader info = null;

            try
            {
                info = this.GetSystemInformation(connectionString);

                int originalIndentLevel = Response.IndentLevel;
                Response.IndentLevel = indentLevel;
                Response.WriteLine("<table width=\"95%\">");

                while (info.Read())
                {
                    Response.WriteLine("<tr>");
                    Response.WriteLine("	<td width=\"40\">&nbsp;</td>");
                    Response.WriteLine("	<td width=\"100\">");
                    Response.WriteLine("		<b><span class=\"bodyText\">" + info["Name"] + ":</span></b>");
                    Response.WriteLine("	</td>");
                    Response.WriteLine("	<td width=\"100%\" align=\"left\">");
                    Response.WriteLine("		<span class=\"bodyText\">" + info["Character_Value"].ToString().Trim() + "</span>");
                    Response.WriteLine("    </td>");
                    Response.WriteLine("</tr>");
                }

                Response.WriteLine("</table>");
                Response.IndentLevel = originalIndentLevel;
            }
            catch (Exception ex)
            {
                this.OutputExceptionInformation(ex, indentLevel);
            }
            finally
            {
                if (info != null)
                {
                    info.Close();
                }
            }
        }

        public void OutputTemplate(CodeTemplate template)
        {
            this.CopyPropertiesTo(template);
            template.Render(this.Response);
        }

        public void RenderBodyWrite(string renderBodyValue)
        {
            this.RenderBodyWrite(renderBodyValue, string.Empty);
        }

        public void RenderBodyWrite(string renderBodyValue, string notRenderBodyValue)
        {
            if (this.RenderBody)
            {
                Response.Write(renderBodyValue);
            }
            else
            {
                if (!string.IsNullOrEmpty(notRenderBodyValue))
                {
                    Response.Write(notRenderBodyValue);
                }
            }
        }

         public void RenderTemplateToFile(CodeSmith.Engine.CodeTemplate template, string destination)
        {
            this.CopyPropertiesTo(template);
            template.RenderToFile(destination, true);            
        }
        
         public void BuildTemplate(CodeTemplate template, string projectLocation, string subFolderPath, string generatedFileName, string parentFileName)
        {
            if (!string.IsNullOrEmpty(parentFileName))
            {
                this.ExecuteTemplate(template, Path.Combine(this.OutputDirectory, this.SolutionName, this.CurrentProjectAlias, subFolderPath, parentFileName));
                this.AddFileToProject(projectLocation, subFolderPath, parentFileName, string.Empty);

                this.ExecuteTemplate(template, Path.Combine(this.OutputDirectory, this.SolutionName, this.CurrentProjectAlias, subFolderPath, generatedFileName));
                this.AddFileToProject(projectLocation, subFolderPath, generatedFileName, parentFileName);
            }
            else
            {
                this.ExecuteTemplate(template, Path.Combine(this.OutputDirectory, this.SolutionName, this.CurrentProjectAlias, subFolderPath, generatedFileName));
                this.AddFileToProject(projectLocation, subFolderPath, generatedFileName, string.Empty);
            }
        }

        #endregion
    }
  
    public class ProjectInfo
    {
        #region Constructors and Destructors

        public ProjectInfo(String alias, Guid projectGuid, ProjectType projectType)
        {
            this.Alias = alias;
            this.ProjectGuid = projectGuid;
            this.ProjectType = projectType;
        }

        #endregion

        #region Public Properties

        public String Alias { get; set; }

        public Guid ProjectGuid { get; set; }

        public ProjectType ProjectType { get; set; }

        #endregion
    }

    public enum ProjectType
    {
        [Description("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}")]
        ASPNET_ONE,

        [Description("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}")]
        WindowsCSharp,

        [Description("{F184B08F-C81C-45F6-A57F-5ABD9991F28F}")]
        WindowsVb,

        [Description("{8BC9CEB8-8B4A-11D0-8D11-00A0C91BC942}")]
        WindowsVisualCpp,

        [Description("{349C5851-65DF-11DA-9384-00065B846F21}")]
        WebApplication,

        [Description("{E24C65DC-7377-472B-9ABA-BC803B73C61A}")]
        WebSite,

        [Description("{F135691A-BF7E-435D-8960-F99683D2D49C}")]
        DistributedSystem,

        [Description("{3D9AD99F-2412-4246-B90B-4EAA41C64699}")]
        WindowsCommunicationFoundation_WCF,

        [Description("{60DC8134-EBA5-43B8-BCC9-BB4BC16C2548}")]
        WindowsPresentationFoundation_WPF,

        [Description("{C252FEB5-A946-4202-B1D4-9916A0590387}")]
        VisualDatabaseTools,

        [Description("{A9ACE9BB-CECE-4E62-9AA4-C7E7C5BD2124}")]
        Database,

        [Description("{4F174C21-8C12-11D0-8340-0000F80270F8}")]
        Database_OtherProjectTypes,

        [Description("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}")]
        Test,

        [Description("{20D4826A-C6FA-45DB-90F4-C717570B9F32}")]
        Legacy_2003_SmartDevice_Cs,

        [Description("{CB4CE8C6-1BDB-4DC7-A4D3-65A1999772F8}")]
        Legacy_2003_SmartDevice_Vb,

        [Description("{4D628B5B-2FBC-4AA6-8C16-197242AEB884}")]
        SmartDevice_Cs,

        [Description("{68B1623D-7FB9-47D8-8664-7ECEA3297D4F}")]
        SmartDevice_Vb,

        [Description("{14822709-B5A1-4724-98CA-57A101D1B079}")]
        Workflow_Cs,

        [Description("{D59BE175-2ED0-4C54-BE3D-CDAA9F3214C8}")]
        Workflow_Vb,

        [Description("{06A35CCD-C46D-44D5-987B-CF40FF872267}")]
        DeploymentMergeModule,

        [Description("{3EA9E505-35AC-4774-B492-AD1749C4943A}")]
        DeploymentCab,

        [Description("{978C614F-708E-4E1A-B201-565925725DBA}")]
        DeploymentSetup,

        [Description("{AB322303-2255-48EF-A496-5904EB18DA55}")]
        DeploymentSmartDeviceCab,

        [Description("{A860303F-1F3F-4691-B57E-529FC101A107}")]
        VisualStudioToolsforApplications_VSTA,

        [Description("{BAA0C2D2-18E2-41B9-852F-F413020CAA33}")]
        VisualStudioToolsforOffice_VSTO,

        [Description("{F8810EC1-6754-47FC-A15F-DFABD2E3FA90}")]
        SharePointWorkflow,

        [Description("{6D335F3A-9D43-41b4-9D22-F6F17C4BE596}")]
        XNA_indows,

        [Description("{2DF5C3F4-5A5F-47a9-8E94-23B4456F55E2}")]
        XNA_XBox,

        [Description("{D399B71A-8929-442a-A9AC-8BEC78BB2433}")]
        XNA_Zune,

        [Description("{EC05E597-79D4-47f3-ADA0-324C4F7C7484}")]
        SharePoint_Vb,

        [Description("{593B0543-81F6-4436-BA1E-4747859CAAE2}")]
        SharePoint_Cs,

        [Description("{A1591282-1198-4647-A2B1-27E5FF5F6F3B}")]
        Silverlight,

        [Description("{603C0E0B-DB56-11DC-BE95-000D561079B0}")]
        ASPNET_MVC1,

        [Description("{F85E285D-A4E0-4152-9332-AB1D724D3325}")]
        ASPNET_MVC2,

        [Description("{E53F8FEA-EAE0-44A6-8774-FFD645390401}")]
        ASPNET_MVC3,

        [Description("{E3E379DF-F4C6-4180-9B81-6769533ABE47}")]
        ASPNET_MVC4,

        [Description("{82B43B9B-A64C-4715-B499-D71E9CA2BD60}")]
        Extensibility
    }
}