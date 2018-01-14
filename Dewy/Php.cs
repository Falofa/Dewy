using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dewy
{
    class Php
    {
        public static PhpLib[] GetLibs()
        {
            List<PhpLib> Libs = new List<PhpLib>();

            Libs.Add(new PhpLib()
            {
                Name = "Bootstrap",
                FileName = "css/bootstrap.min.css",
                Url = new Uri("https://maxcdn.bootstrapcdn.com/bootstrap/3.3.7/css/bootstrap.min.css")
            });

            Libs.Add(new PhpLib()
            {
                Name = "JQuery",
                FileName = "js/jquery.min.js",
                Url = new Uri("https://cdnjs.cloudflare.com/ajax/libs/jquery/3.2.1/jquery.min.js")
            });

            Libs.Add(new PhpLib()
            {
                Name = "Bootstrap JS",
                FileName = "js/bootstrap.min.js",
                Url = new Uri("https://maxcdn.bootstrapcdn.com/bootstrap/3.3.7/js/bootstrap.min.js")
            }); 

            Libs.Add(new PhpLib()
            {
                Name = "JQuery UI",
                FileName = "js/jquery-ui.min.js",
                Url = new Uri("https://code.jquery.com/ui/1.12.1/jquery-ui.min.js")
            });

            Libs.Add(new PhpLib()
            {
                Name = "Underscore JS",
                FileName = "js/underscore-min.js",
                Url = new Uri("https://cdnjs.cloudflare.com/ajax/libs/underscore.js/1.8.3/underscore-min.js")
            });
            return Libs.ToArray();
        }
        public static Dictionary<string, string> Imports = new Dictionary<string, string>
        {
            { "css", "<link rel=\"stylesheet\" type=\"text/css\" href=\"{0}\">" },
            { "js",  "<script type=\"text/javascript\" src=\"{0}\"></script>" },
            { "ico", "<link rel=\"shortcut icon\" href=\"{0}\">" },
            { "png", "<link rel=\"icon\" type=\"image/png\" href=\"{0}\">" },
        };
        public static string[] Folders = new string[]
        {
            "ajax",
            "css",
            "img",
            "js",
            "sys"
        };
        public static string Header = @"<!DOCTYPE html PUBLIC ""-//W3C//DTD XHTML 1.0 Transitional//EN"" ""http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd"">
<head>
    <title><?php echo isset($title) ? $title : """"; ?></title>

{0}";
        public static string Htaccess = "Options -Indexes";
        public static string BlockHta = "Deny From All";
        public static string Template = @"<?php
    $title = ""{0}"";
    include(""sys/header.php"");
?>
        <link rel=""stylesheet"" type=""text/css"" href=""css/{0}.css"" >
        <script type=""text/javascript"" src=""js/{0}.js""></script>
    </head>
    <body>
    
    </body>
</html>";
        public static Dictionary<string, string> BaseFiles = new Dictionary<string, string>()
        {
            { ".htaccess", Htaccess },
            //{ "sys/header.php", Header },
            { "sys/.htaccess", BlockHta },
            { "template.php", Template },
            { "js/template.js", string.Empty },
            { "css/template.css", string.Empty },
        };
    }
    class PhpLib
    {
        public string Name;
        public string FileName;
        public Uri Url;
    }
}
