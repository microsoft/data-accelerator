// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/// <summary>
/// Diable warning for the naming rule for this file: # Public members must be capitalized
/// </summary>
#pragma warning disable IDE1006
namespace DataX.Flow.CodegenRules
{
    public class Input
    {
        public Input(string name)
        {
            metricKeys = new List<MetricKey>();
            pollingInterval = 60000;

            if (name.ToLower().Contains("alert") && !name.Contains(","))
            {
                type = "MetricDetailsApi";
            }
            else
            {
                type = "MetricApi";
            }

            string[] names = name.Split(',');
            if (names != null && names.Length > 0)
            {
                foreach (string n in names)
                {
                    MetricKey mk = new MetricKey
                    {
                        name = $"_FLOW_:{n.Trim()}",
                        displayName = $"{n.Trim()}"
                    };
                    metricKeys.Add(mk);
                }
            }
        }

        public string type { get; set; }
        public int pollingInterval { get; set; }
        public List<MetricKey> metricKeys { get; set; }
    }

    public class MetricKey
    {
        public string name { get; set; }
        public string displayName { get; set; }
    }

    public class Data
    {
        public Data()
        {
            timechart = true;
            current = false;
            table = false;
        }

        public bool timechart { get; set; }
        public bool current { get; set; }
        public bool table { get; set; }
    }

    public class Output
    {
        public Output(string name)
        {
            data = new Data();
            chartTimeWindowInMs = 3600000;

            if (name.ToLower().Contains("alert") && !name.Contains(","))
            {
                type = "DirectTable";
                data.timechart = false;
                data.table = true;
            }
            else
            {
                type = "DirectTimeChart";
                data.timechart = true;
                data.table = false;
            }
        }

        public string type { get; set; }
        public Data data { get; set; }
        public int chartTimeWindowInMs { get; set; }
    }

    public class Source
    {
        public Source(string name)
        {
            input = new Input(name);
            output = new Output(name);
            this.name = name;
        }

        public string name { get; set; }
        public Input input { get; set; }
        public Output output { get; set; }
    }

    public class Widget
    {
        public Widget(string name)
        {
            this.name = name;
            position = "TimeCharts";
            if (name.ToLower().Contains("alert") && !name.Contains(","))
            {
                type = "DetailsList";
                data = name + "_table";
            }
            else
            {
                type = "MultiLineChart";
                data = name + "_timechart";
            }

        }

        public string name { get; set; }
        public string data { get; set; }

        public string displayName
        {
            get => name;
            set => displayName = value;
        }

        public string position { get; set; }
        public string type { get; set; }
    }

    public class JobNames
    {
        public JobNames()
        {
            type = "getCPSparkJobNames";
        }

        public string type { get; set; }
    }

    public class InitParameters
    {
        public InitParameters()
        {
            widgetSets = new List<string>() { "direct" };
            jobNames = new JobNames();
        }

        public IList<string> widgetSets { get; set; }
        public JobNames jobNames { get; set; }
    }

    public class Metrics
    {
        public Metrics()
        {
            sources = new List<Source>();
            widgets = new List<Widget>();
            initParameters = new InitParameters();
        }

        public void AddMetric(string name)
        {
            sources.Add(new Source(name));
            widgets.Add(new Widget(name));
        }

        public IList<Source> sources { get; set; }
        public IList<Widget> widgets { get; set; }
        public InitParameters initParameters { get; set; }
    }

    public class MetricsRoot
    {
        public MetricsRoot()
        {
            metrics = new Metrics();
        }

        public void AddMetric(string name)
        {
            metrics.AddMetric(name);
        }

        public Metrics metrics { get; set; }
    }

}
