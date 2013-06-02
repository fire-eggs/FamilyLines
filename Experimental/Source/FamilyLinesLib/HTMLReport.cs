using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

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

        private void openReport(string htmlFilePath)
        {
            tw = new StreamWriter(Path.GetFileName(htmlFilePath));
        }

        protected void outputHeader( string title )
        {
            tw.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            tw.WriteLine("<!DOCTYPE html PUBLIC \"-//W3C//DTD XHTML 1.0 Transitional//EN\" \"http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd\">");
            tw.WriteLine("<html xmlns=\"http://www.w3.org/1999/xhtml\" xml:lang=\"en\" lang=\"en\">");
            tw.WriteLine("<head>");
            tw.WriteLine("<title>" + title + "</title>");
        }

        protected void outputFooter()
        {
            Version version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            string versionlabel = string.Format(CultureInfo.CurrentCulture, "{0}.{1}.{2}", version.Major, version.Minor, version.Build);
            string date = DateTime.Now.ToString();
            tw.WriteLine("</table><br/><p><i>" + Properties.Resources.GeneratedByFamilyShow + " " + versionlabel + " " + Properties.Resources.On + " " + date + "</i></p></body></html>");
            tw.Close();
        }

        protected void outputBody(bool showHide = false)
        {
            tw.Write("<body");
            if (showHide)
            {
                tw.WriteLine("onload=\"javascript:showhideall('hide_all')\">");
                tw.WriteLine("<input type=\"button\" onclick=\"showhideall('hide_all')\" value=\"Hide all notes\" />");
                tw.WriteLine("<input type=\"button\" onclick=\"showhideall('show_all')\" value=\"Show all notes\" />");
            }
            else
            {
                tw.WriteLine(">");
            }
        }

        protected void outputCSS()
        {
            tw.WriteLine(Properties.Resources.PeopleReportCSS);
        }

        protected void showHideScript()
        {
            tw.WriteLine("<script type=\"text/javascript\">");

            tw.WriteLine("function showhide(id,thing) {");
	        tw.WriteLine("    var person = document.getElementById(id);");
	        tw.WriteLine("    var note = document.getElementById(thing+'_'+id);");
	        tw.WriteLine("    if (note.className == 'noteshow') {");
		    tw.WriteLine("        note.className = 'notehide';");
		    tw.WriteLine("        person.className = 'person';");
	        tw.WriteLine("    } else {");
		    tw.WriteLine("        note.className = 'noteshow';");
		    tw.WriteLine("        person.className = 'personhighlight'; }");
            tw.WriteLine("}");
//function showhideall(hide) {
//var allTags=document.getElementsByTagName('tr');
//for (i=0; i<allTags.length; i++) {
//if(hide=='hide_all'){
//if (allTags[i].className=='noteshow') {
//    allTags[i].className='notehide';
//    }
//if (allTags[i].className=='personhighlight') {
//    allTags[i].className='person';
//    }	
//}
//if(hide=='show_all'){
//if (allTags[i].className=='notehide') {
//    allTags[i].className='noteshow';
//    allTags[i-1].className='personhighlight'; 	
//    }
//}
//}
//}
            tw.WriteLine("</script>");
        }

        protected void finishTable()
        {
            tw.WriteLine("</table>");
        }

    }
}
