﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace XAMLator
{
	public class XAMLDocument
	{
		public XAMLDocument(string filePath, string xaml, string type, List<string> styleSheets)
		{
			FilePath = filePath;
			XAML = xaml;
			Type = type;
			StyleSheets = styleSheets;
		}

		public string XAML { get; set; }

		public string Type { get; set; }

		public string FilePath { get; set; }

		public List<string> StyleSheets { get; set; }

		public static XAMLDocument Parse(string filePath, string xaml)
		{
			try
			{
				using (var stream = new StringReader(xaml))
				{
					var reader = XmlReader.Create(stream);
					var xdoc = XDocument.Load(reader);
					XNamespace x = "http://schemas.microsoft.com/winfx/2009/xaml";
					XNamespace xm = "http://xamarin.com/schemas/2014/forms";
					var classAttribute = xdoc.Root.Attribute(x + "Class");
					CleanAutomationIds(xdoc.Root);
					var styleSheets = xdoc.Root
										   .Descendants()
										   .Where(e => e.Name.ToString().EndsWith("StyleSheet"))
										   .Select(e => e.Attribute("Source").Value)
										   .ToList();
					xaml = xdoc.ToString();
					return new XAMLDocument(filePath, xaml, classAttribute.Value, styleSheets);
				}
			}
			catch (Exception ex)
			{
				Log.Exception(ex);
				return null;
			}
		}

		static void CleanAutomationIds(XElement xdoc)
		{
			xdoc.SetAttributeValue("AutomationId", null);
			foreach (var el in xdoc.Elements())
			{
				CleanAutomationIds(el);
			}
		}
	}
}
