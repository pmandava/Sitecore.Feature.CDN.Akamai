// MIT License
// 
// Copyright (c) 2017 Purnima Mandava
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
namespace Sitecore.Feature.CDN
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Text;
    using Data.Items;
    using Foundation.CDN;
    using Abstractions;
    
    public class AkamaiDeliveryService : IDeliveryService
    {
        /// <summary>
        /// Gets the Path Service that generates Cdn Urls
        /// </summary>
        private readonly IPathService pathService;

        /// <summary>
        /// Sitecore Logger Implementation
        /// </summary>
        private readonly BaseLog logger;

        /// <summary>
        /// Gets the Akamai specific settings
        /// </summary>
        private readonly IAkamaiSettings akamaiSettings;
       

        public AkamaiDeliveryService(IPathService pathService, IAkamaiSettings akamaiSettings, BaseLog logger)
        {
            if (String.IsNullOrEmpty(akamaiSettings.Username)
                || String.IsNullOrEmpty(akamaiSettings.Password)
                || String.IsNullOrEmpty(akamaiSettings.Queue)
                || String.IsNullOrEmpty(akamaiSettings.APIpoint))
            {
                throw new ArgumentException($"{nameof(akamaiSettings)} is not fully instantiated.");
            }

            this.pathService = pathService;
            this.akamaiSettings = akamaiSettings;
            this.logger = logger;
        }

        /// <summary>
        /// Purges the Items from the Akamai CDN
        /// </summary>
        /// <param name="items">The items that have require purging</param>
        public virtual async void Purge(IEnumerable<Item> items)
        {
            var list = items as Item[] ?? items.ToArray();

            if (!list.Any())
            {
                return;
            }

            var urls = list.SelectMany(item => this.pathService.GeneratePaths(item))
                           .Distinct()
                           .ToArray();

            if (urls.Contains("/"))
            {
                var temp = urls.ToList();

                temp.Remove("/");

                urls = temp.ToArray();
            }

            //Need to Purge Content
           
            var filesToPurge = urls.ToArray(); //this is the item in akamai proxy that you want to purge
           
            try
            {
             
                //Using REST API Refer : https://developer.akamai.com/api/purge/ccu-v2/overview.html

                //Akamai Purge Object
                var purgeobject = new AkamaiPurge()
                {
                    objects = filesToPurge,
                    type = this.akamaiSettings.Type,
                    domain = this.akamaiSettings.Domain,
                    action = this.akamaiSettings.Action
                };

                //Using HTTP Client

                var httpClient = new HttpClient();
                var resourceAddress = this.akamaiSettings.APIpoint + this.akamaiSettings.Queue;
                var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes(this.akamaiSettings.Username + ":" + this.akamaiSettings.Password));

                //Adding Basic Authentication
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", credentials);
                httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                var response = await httpClient.PostAsync(resourceAddress, new StringContent(purgeobject.ToString(), Encoding.UTF8, "application/json"));

                if (!response.IsSuccessStatusCode)
                {
                    this.logger.Error($"Purge Request failed with reason : {response.ReasonPhrase}", this);
                    this.logger.Error($"Purge Request for these items failed :\n {String.Join("\n", filesToPurge)}", this);
                }

            }
            catch (Exception ex)
            {
                this.logger.Error($"Purge Exception : {ex.Message}", this);
            }

        }
    }
}