// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace DataX.Config.ConfigDataModel.RuntimeConfig
{
  public  class FlowBlobOutputSpec
  {
        [JsonProperty("groups")]
        public BlobOutputGroups Groups { get; set; }

        [JsonProperty("compressionType")]
        public string CompressionType { get; set; }

        [JsonProperty("format")]
        public string Format { get; set; }
    }

    public class BlobOutputGroups
    {
        [JsonProperty("main")]
        public BlobOutputMain Main { get; set; }
    }

    public class BlobOutputMain
    {
        [JsonProperty("folder")]
        public string Folder { get; set; }
    }
}


