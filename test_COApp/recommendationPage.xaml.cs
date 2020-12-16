using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace test_COApp
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public class recJson
    {
        public string recommendationDate { get; set; }
        public string lookback { get; set; }
    }

    public class flyMonthDataModel
    {
        public string Fly { get; set; }
        public double Actual { get; set; }
        public double Predicted { get; set; }
        public double SDsAways { get; set; }
        public string Value { get; set; }
        public string FlyMonth { get; set; }
    }
    public class DflyMonthDataModel
    {
        public string Structure { get; set; }
        public double Actual { get; set; }
        public double Predicted { get; set; }
        public double SDsAways { get; set; }
        public string Value { get; set; }
        public string Month { get; set; }
    }
    public class flyViewModel
    {
        public ObservableCollection<flyMonthDataModel> fly { get; set; }
    }
    public class DflyViewModel
    {
        public ObservableCollection<DflyMonthDataModel> doubleFly { get; set; }
    }
    public partial class recommendationPage : ContentPage
    {
        public recommendationPage()
        {
            InitializeComponent();

            var lookBack = new List<string>();
            lookBack.Add("1");
            lookBack.Add("3");
            lookBack.Add("6");
            lookBack.Add("9");
            lookBack.Add("12");
            lookBack.Add("All");
            lookbackWindow.ItemsSource = lookBack;
            lookbackWindow.SelectedIndex = 0;
        }

        async private void recButton_Clicked(object sender, EventArgs e)
        {
            Debug.WriteLine("inside button event");
            Debug.WriteLine("inside if statement");
            try
            {
                Debug.WriteLine("inside try statement");
                var value = new recJson()
                {
                    recommendationDate = recDate.Date.ToString("yyyy-MM-dd"),
                    lookback = lookbackWindow.SelectedItem.ToString()
                };
                Debug.WriteLine("value created");

                var client = new HttpClient();
                client.BaseAddress = new Uri("http://ec2-3-94-188-74.compute-1.amazonaws.com:5000");

                Debug.WriteLine("client object created");

                string jsondata = JsonConvert.SerializeObject(value);

                Debug.WriteLine("value deserialised to be sent");

                var content = new StringContent(jsondata, Encoding.UTF8, "application/json");

                Debug.WriteLine("sending request");
                    
                HttpResponseMessage response = await client.PostAsync("/recommendation", content);
                var json = await response.Content.ReadAsStringAsync();
                Debug.WriteLine("json ---->" + json);
                Debug.WriteLine("json string ---->" + json.ToString());
                var flyCollection = JsonConvert.DeserializeObject<flyViewModel>(json);
                var DflyCollection = JsonConvert.DeserializeObject<DflyViewModel>(json);

                Debug.WriteLine("showing data on app");

                flyTable.ItemsSource = flyCollection.fly;
                DflyTable.ItemsSource = DflyCollection.doubleFly;


                Debug.WriteLine("data sent to app");

            }
            catch (Exception r)
            {
                Debug.WriteLine(r);
            }
            finally
            {
                Debug.WriteLine("ran Recommendations!");
            }
            
        }
    }
}