/*
 * Family Lines code is provided using the Apache License V2.0, January 2004 http://www.apache.org/licenses/
 * 
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace KBS.FamilyLinesLib
{
    /// <summary>
    /// Create default styles, in raw xml format.
    /// </summary>
    public static class KmlStyleFactory
    {
        public static List<XElement> CreateStylesForMale()
        {
            var result = new List<XElement>();

            result.Add(XElement.Parse(
                @"<Style id='sn_man2'>
                    <IconStyle>
                        <scale>0.9</scale>
                        <Icon>
                            <href>http://maps.google.com/mapfiles/kml/shapes/man.png</href>
                        </Icon>
                    </IconStyle>
                    <LabelStyle>
                        <scale>0.9</scale>
                    </LabelStyle>
                  </Style>"));
            result.Add(XElement.Parse(
                @"<Style id='sh_man0'>
                    <IconStyle>
                        <scale>0.9</scale>
                        <Icon>
                            <href>http://maps.google.com/mapfiles/kml/shapes/man.png</href>
                        </Icon>
                    </IconStyle>
                    <LabelStyle>
                        <scale>0.9</scale>
                    </LabelStyle>
                  </Style>"));
            result.Add(XElement.Parse(
                 @"<StyleMap id='msn_man'>
                    <Pair>
                        <key>normal</key>
                        <styleUrl>#sn_man2</styleUrl>
                    </Pair>
                    <Pair>
                        <key>highlight</key>
                        <styleUrl>#sh_man0</styleUrl>
                    </Pair>
                   </StyleMap>"));
            return result;
        }

        public static List<XElement> CreateStylesForFemale()
        {
            var result = new List<XElement>();

            result.Add(XElement.Parse(
                @"<Style id='sn_woman1'>
                    <IconStyle>
                        <scale>0.9</scale>
                        <Icon>
                            <href>http://maps.google.com/mapfiles/kml/shapes/woman.png</href>
                        </Icon>
                    </IconStyle>
                    <LabelStyle>
                        <scale>0.9</scale>
                    </LabelStyle>
                  </Style>"));
            result.Add(XElement.Parse(
                @"<Style id='sh_woman0'>
                    <IconStyle>
                        <scale>0.9</scale>
                        <Icon>
                            <href>http://maps.google.com/mapfiles/kml/shapes/woman.png</href>
                        </Icon>
                    </IconStyle>
                    <LabelStyle>
                        <scale>0.9</scale>
                    </LabelStyle>
                  </Style>"));
            result.Add(XElement.Parse(
                 @"<StyleMap id='msn_woman'>
                    <Pair>
                        <key>normal</key>
                        <styleUrl>#sn_woman1</styleUrl>
                    </Pair>
                    <Pair>
                        <key>highlight</key>
                        <styleUrl>#sh_woman0</styleUrl>
                    </Pair>
                   </StyleMap>"));
            return result;
        }
    }
}
