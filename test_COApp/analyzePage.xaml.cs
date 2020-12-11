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
    public class RegressionDetails
    {
        public string equation { get; set; }
        public string PolyEquation { get; set; }
        public double rsquared { get; set; }
        public double rsquaredPoly { get; set; }
        public double standardError { get; set; }
        public double standardErrorPoly { get; set; }
    }
    public class graphData
    {
        public double Scattery { get; set; }
        public double Chartx { get; set; }
        public string Date { get; set; }
        public double PolyRegressiony { get; set; }
        public double StandardErrorPoly { get; set; }
        public double Regressiony { get; set; }
        public double Standard { get; set; }
    }
    public class RegressioDetailsViewModel
    {
        public ObservableCollection<RegressionDetails> RegressionDetails { get; set; }
    }
    public class graphDataViewModel
    {
        public ObservableCollection<graphData> GraphData { get; set; }
    }
    public class portfolioData
    {
        public string Qty { get; set; }
        public string contract { get; set; }
    }
    public class AnalyzeDataPostJson
    {
        public string asOnDate { get; set; }
        public List<Dictionary<string, string>> portfolio { get; set; }
        public string lookback { get; set; }
        public string graphType { get; set; }
        public string fly { get; set; }
    }
    public class activeflylist
    {
        public string value { get; set; }
    }
    public class activeflylistViewModel
    {
        public ObservableCollection<activeflylist> data { get; set; }
    }
    public class activeflylistJson
    {
        public string asOnDate { get; set; }
    }

    public partial class analyzePage : ContentPage
    {
        List<string> populateContract = new List<string>();
        string equation = "";
        int buyCount = 0;
        int sellCount = 0;
        List<string> portfolioContracts = new List<string>() { "test" };
        List<Dictionary<string, string>> portfoliojson = new List<Dictionary<string, string>>();
        public analyzePage()
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

            var buyOrSell = new List<string>();
            buyOrSell.Add("Buy");
            buyOrSell.Add("Sell");
            buySell.ItemsSource = buyOrSell;
            buySell.SelectedIndex = 0;

            pickContract.IsEnabled = false;
            portfolioEquation.Text = equation;

            graphtype1.ItemsSource = new List<string>() 
            {
                "Combined vs Outright",
                "Fly vs Fly Regression"
            };
            graphtype1.SelectedIndex = 0;
            graphtype2.ItemsSource = new List<string>()
            {
                "Fly vs Average Outright",
                "Fly vs Outright"
            };
            graphtype2.SelectedIndex = 0;
        }

        async private void asOnDate_DateSelected(object sender, DateChangedEventArgs e)
        {
            var selected_date = new activeflylistJson()
            {
                asOnDate = e.NewDate.ToString("yyyy-MM-dd")
            };
            var client = new HttpClient();
            client.BaseAddress = new Uri("http://192.168.1.169:5000");

            string jsondata = JsonConvert.SerializeObject(selected_date);
            var content = new StringContent(jsondata, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await client.PostAsync("/activeflylist", content);

            var json = await response.Content.ReadAsStringAsync();

            string transformedJson = @"{'data':" + json + "}";

            var activeflylist_result = JsonConvert.DeserializeObject<activeflylistViewModel>(transformedJson);
            
            for (var i = 0; i < activeflylist_result.data.Count; i++)
            {
                populateContract.Add(activeflylist_result.data.ElementAt(i).value);
            }

            pickContract.ItemsSource = populateContract;
            pickContract.IsEnabled = true;
        }

        async private void Add_Button_Clicked(object sender, EventArgs e)
        {
            if (buySell.SelectedItem.ToString() == "Buy")
            {
                if (buyCount < 1)
                {
                    string temp = "( 1 * " + pickContract.SelectedItem.ToString().Substring(0, 11) + " )";
                    equation += temp;
                    buyCount += 1;
                    portfolioEquation.Text = equation;
                    portfolioContracts.Add(pickContract.SelectedItem.ToString().Substring(0, 11));
                    fly.ItemsSource = portfolioContracts;
                    fly.SelectedIndex = 1;
                }
                else
                {
                    await DisplayAlert("Warning", "You have already bought one contract", "OK");
                }
            }
            else
            {
                if (sellCount < 1)
                {      
                    string temp = "( -1 * " + pickContract.SelectedItem.ToString().Substring(0, 11) + " )";
                    equation += temp;
                    sellCount += 1;
                    portfolioEquation.Text = equation;
                    portfolioContracts.Add(pickContract.SelectedItem.ToString().Substring(0, 11));
                    fly.ItemsSource = portfolioContracts;
                    fly.SelectedIndex = 1;
                }
                else
                {
                    await DisplayAlert("Warning", "You have already sold one contract", "OK");
                }
            }
                                    
        }

        async private void Analyze_Button_Clicked(object sender, EventArgs e)
        {

            Dictionary<string, string> temp = new Dictionary<string, string>()
            {
                {"Qty", "1"},
                {"contract", portfolioContracts[1] }
            };
            portfoliojson.Add(temp);

            Dictionary<string, string> temp1 = new Dictionary<string, string>()
            {
                {"Qty", "-1"},
                {"contract", portfolioContracts[2] }
            };
            portfoliojson.Add(temp1);

            var value = new AnalyzeDataPostJson()
            {
                asOnDate = asOnDate.Date.ToString("yyyy-MM-dd"),
                portfolio = portfoliojson,
                lookback = lookbackWindow.SelectedItem.ToString(),
                graphType = graphtype1.SelectedItem.ToString(),
                fly = fly.SelectedItem.ToString()
            };

            var client = new HttpClient();
            client.BaseAddress = new Uri("http://192.168.1.169:5000");

            string jsondata = JsonConvert.SerializeObject(value);
            var content = new StringContent(jsondata, Encoding.UTF8, "application/json");

            Debug.WriteLine("content before sending api call -----> " + content);

            HttpResponseMessage response = await client.PostAsync("/analyzegraphdata", content);

            var json = await response.Content.ReadAsStringAsync();
            Debug.WriteLine("json after sending api call -----> " + json);
            var regressionDataCollection = JsonConvert.DeserializeObject<RegressioDetailsViewModel>(json);
            Debug.WriteLine("regression data deserialized");
            var graphDataCollection = JsonConvert.DeserializeObject<graphDataViewModel>(json);

            Debug.WriteLine(regressionDataCollection);
            Debug.WriteLine(graphDataCollection);

        }
    }
}