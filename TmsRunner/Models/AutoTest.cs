﻿using Tms.Adapter.Models;

namespace TmsRunner.Models
{
    public class AutoTest
    {
        public string Namespace { get; set; }
        public string Classname { get; set; }
        public List<AutoTestStep> Steps { get; set; } = new();
        public List<AutoTestStep> Setup { get; set; }
        public List<AutoTestStep> Teardown { get; set; }
        public string ExternalId { get; set; }
        public string Name { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public List<string> WorkItemIds { get; set; } = new();
        public List<Link>? Links { get; set; } = new();
        public List<string>? Labels { get; set; }
        public string MethodName { get; set; }
        public string? Message { get; set; }
        public bool? IsFlaky { get; set; }
    }
}