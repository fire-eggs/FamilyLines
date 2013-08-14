/*
 * Family Lines code is provided using the Apache License V2.0, January 2004 http://www.apache.org/licenses/
 * 
 * Attempts to provide "standard" HTML output support for multiple derived reports.
 */
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using KBS.FamilyLinesLib.Properties;

namespace KBS.FamilyLinesLib
{
    public class HTMLReport
    {
        protected StreamWriter tw;
        protected IEnumerable<Person> people;

        protected HTMLReport(string outPath, IEnumerable<Person> enumerable)
        {
            openReport(outPath);
            people = enumerable;
        }

        public bool Privacy { get; set; }

        private void openReport(string htmlFilePath)
        {
            tw = new StreamWriter(Path.GetFileName(htmlFilePath));
        }

        /// <summary>
        /// Generic header w/ standard doctype, html, head tags.
        /// </summary>
        /// <param name="title">The title of the HTML document</param>
        protected void outputHeader( string title )
        {
            tw.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            tw.WriteLine("<!DOCTYPE html PUBLIC \"-//W3C//DTD XHTML 1.0 Transitional//EN\" \"http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd\">");
            tw.WriteLine("<html xmlns=\"http://www.w3.org/1999/xhtml\" xml:lang=\"en\" lang=\"en\">");
            tw.WriteLine("<head>");
            tw.WriteLine("<title>" + title + "</title>");
        }

        /// <summary>
        /// Generic footer displaying the product name and version #
        /// </summary>
        protected void outputFooter()
        {
            Version version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            string versionlabel = string.Format(CultureInfo.CurrentCulture, "{0}.{1}.{2}", version.Major, version.Minor, version.Build);
            string date = DateTime.Now.ToString();

            // TODO localization needs to be corrected
            tw.WriteLine("</table><br/><p><i>" + Resources.GeneratedByFamilyShow + " " + versionlabel + " " + Resources.On + " " + date + "</i></p></body></html>");
        }

        protected void outputBody(string tableName, bool showHide = false, bool stripe = false)
        {
            tw.Write("<body");
            if (showHide)
            {
                tw.Write(" onload=\"javascript:showhideall('hide_all','note');");
                if (stripe)
                    tw.Write("stripe('"+ tableName + "');");
                tw.WriteLine("\">");
                tw.WriteLine("<input type=\"button\" onclick=\"showhideall('hide_all','event')\" value=\"Hide all events\" />");
                tw.WriteLine("<input type=\"button\" onclick=\"showhideall('show_all','event')\" value=\"Show all events\" />");
                tw.WriteLine("<input type=\"button\" onclick=\"showhideall('hide_all','fact')\" value=\"Hide all facts\" />");
                tw.WriteLine("<input type=\"button\" onclick=\"showhideall('show_all','fact')\" value=\"Show all facts\" />");
                tw.WriteLine("<input type=\"button\" onclick=\"showhideall('hide_all','note')\" value=\"Hide all notes\" />");
                tw.WriteLine("<input type=\"button\" onclick=\"showhideall('show_all','note')\" value=\"Show all notes\" />");
            }
            else
            {
                if (stripe)
                    tw.Write(" onload=\"javascript:stripe('" + tableName + "');\"");
                tw.WriteLine(">");
            }
        }

        protected void outputCSS()
        {
            tw.WriteLine(Resources.PeopleReportCSS);
        }

        protected void showHideScript()
        {
            tw.WriteLine(Resources.PeopleReportShowHideScript);
        }

        protected void finishTable()
        {
            tw.WriteLine("</table>");
        }

    }
}
