﻿using System.Collections.Generic;

namespace SpecNuts.Model
{
	public class Feature : TaggedReportItem
	{
		private string _description;

		private static readonly string DefaultUri = "/uri/placeholder";

		public string Description
		{
			get { return _description; }
			set { _description = string.IsNullOrEmpty(value) ? value : value.Replace("\r", ""); }
		}
		public string Uri { get; set; } = DefaultUri;

		public List<Scenario> Elements { get; set; }
		public new string Keyword => "Feature";
	}
}