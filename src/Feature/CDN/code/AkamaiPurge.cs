﻿using System.Collections.Generic;


namespace Sitecore.Feature.CDN
{
  
    public class AkamaiPurge 
    {
        /// <summary>
        /// Gets or sets the objects
        /// </summary>
        public IEnumerable<string> objects { get; set; }

        /// <summary>
        /// Gets or sets the action
        /// </summary>
        public string action { get; set; }

        /// <summary>
        /// Gets or sets the type
        /// </summary>
        public string type { get; set; }

        /// <summary>
        /// Gets or sets the domain
        /// </summary>
        public string domain { get; set; }

    }
}