using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookExtractor.Models {
    internal class Settings {
        //Service selector, model, api key, prompt text
        public string Service { get; set; }
        public string Model { get; set; }
        public string ApiKey { get; set; }
        public string PromptText { get; set; }
    }
}
