using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SkiaSharp;
using Syncfusion.SfChart.XForms;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;


namespace test_COApp
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public class GraphModel
    {
        public string category { get; set; }
        public double settle { get; set; }
    }
    public class GraphViewModel
    {
        public ObservableCollection<GraphModel> GraphView { get; set; }
    }
    public class json
    {
        public string base_date { get; set; }
        public string lookForward { get; set; }
        public List<string> compare_dates { get; set; }
        public string is_settle { get; set; }
        public int monthOf { get; set; }
        public string type { get; set; }

    }
    public partial class forwardCurvesPage : ContentPage
    {
        async private void DataProvider(json inputjson)
        {
            var values = inputjson;
            var client = new HttpClient();
            client.BaseAddress = new Uri("http://192.168.1.169:5000");

            string jsondata = JsonConvert.SerializeObject(values);
            var content = new StringContent(jsondata, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await client.PostAsync("/test", content);

            var json = await response.Content.ReadAsStringAsync();

            string transformedJson = @"{'GraphView':" + json + "}";

            var jsonDataCollection = JsonConvert.DeserializeObject<GraphViewModel>(transformedJson);
            BindingContext = jsonDataCollection;

        }

        public forwardCurvesPage()
        {
            InitializeComponent();
            base_date.Date = new DateTime(2020, 10, 8);

            var baseDate = base_date.Date.ToString("yyyy-MM-dd");
            var look_forward = LookForward.Value.ToString();

            var val = new json()
            {
                base_date = baseDate,
                lookForward = look_forward,
                compare_dates = new List<string>() { },
                is_settle = "true",
                monthOf = 1,
                type = "S"
            };

            DataProvider(val);
        }

        private void base_date_DateChanged(object sender, DateChangedEventArgs e)
        {
            var baseDate = e.NewDate.ToString("yyyy-MM-dd");
            var look_forward = LookForward.Value.ToString();
            
            var val = new json()
            {
                base_date = baseDate,
                lookForward = look_forward,
                compare_dates = new List<string>() { },
                is_settle = "true",
                monthOf = 1,
                type = "S"
            };

            DataProvider(val);
        }

        private void LookForward_ValueChanged(object sender, ValueChangedEventArgs e)
        {
            var baseDate = base_date.Date.ToString("yyyy-MM-dd");
            var look_forward = LookForward.Value.ToString();

            var val = new json()
            {
                base_date = baseDate,
                lookForward = look_forward,
                compare_dates = new List<string>() { },
                is_settle = "true",
                monthOf = 1,
                type = "S"
            };

            DataProvider(val);
        }
    }
}