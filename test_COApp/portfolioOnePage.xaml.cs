using Newtonsoft.Json;
using Syncfusion.SfChart.XForms;
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
    public class CPRegressionDetails
    {
        public string equation { get; set; }
        public string PolyEquation { get; set; }
        public double rsquared { get; set; }
        public double rsquaredPoly { get; set; }
        public double standardError { get; set; }
        public double standardErrorPoly { get; set; }
    }
    public class CPgraphData
    {
        public double Scattery { get; set; }
        public double Chartx { get; set; }
        public string Date { get; set; }
        public double PolyRegressiony { get; set; }
        public double StandardErrorPoly { get; set; }
        public double Regressiony { get; set; }
        public double Standard { get; set; }
    }
    public class CPRegressioDetailsViewModel
    {
        public ObservableCollection<CPRegressionDetails> RegressionDetails { get; set; }
    }
    public class CPgraphDataViewModel
    {
        public ObservableCollection<CPgraphData> GraphData { get; set; }
    }
    public class CPDataPostJson
    {
        public string asOnDate { get; set; }
        public List<Dictionary<string, string>> portfolio { get; set; }
        public string lookback { get; set; }
        public string graphType { get; set; }
    }
    public class CPactiveflylist
    {
        public string value { get; set; }
    }
    public class CPactiveflylistViewModel
    {
        public ObservableCollection<CPactiveflylist> data { get; set; }
    }
    public class CPactiveflylistJson
    {
        public string asOnDate { get; set; }
    }
    public partial class portfolioOnePage : ContentPage
    {
        List<string> populateContract = new List<string>();
        string equation = "";
        List<string> portfolioContracts = new List<string>();
        List<string> portfolioContractsQty = new List<string>();
        public portfolioOnePage()
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

            graphtype.ItemsSource = new List<string>()
            {
                "Portfolio vs Front Outright",
                "5 day change in Portfolio vs 5 day change in Outright",
                "10 day change in Portfolio vs 10 day change in Outright"
            };
            graphtype.SelectedIndex = 0;
        }

        async private void asOnDate_DateSelected(object sender, DateChangedEventArgs e)
        {
            var selected_date = new CPactiveflylistJson()
            {
                asOnDate = e.NewDate.ToString("yyyy-MM-dd")
            };
            var client = new HttpClient();
            client.BaseAddress = new Uri("http://ec2-3-94-188-74.compute-1.amazonaws.com:5000");

            string jsondata = JsonConvert.SerializeObject(selected_date);
            var content = new StringContent(jsondata, Encoding.UTF8, "application/json");
             

            HttpResponseMessage response = await client.PostAsync("/activeflylist", content);

            var json = await response.Content.ReadAsStringAsync();

            string transformedJson = @"{'data':" + json + "}";

            var activeflylist_result = JsonConvert.DeserializeObject<CPactiveflylistViewModel>(transformedJson);

            for (var i = 0; i < activeflylist_result.data.Count; i++)
            {
                populateContract.Add(activeflylist_result.data.ElementAt(i).value);
            }

            pickContract.ItemsSource = populateContract;
            pickContract.IsEnabled = true;
        }

        private void Add_Button_Clicked(object sender, EventArgs e)
        {
            if (buySell.SelectedItem.ToString() == "Buy")
            {
                var c = pickContract.SelectedItem.ToString().Substring(0, 11);
                var q = qty.Value.ToString();

                portfolioContracts.Add(c);
                portfolioContractsQty.Add(q);

                string temp = "( " + q + " * " + c + " )";
                equation += temp;
                portfolioEquation.Text = equation;
            }
            else
            {
                var c = pickContract.SelectedItem.ToString().Substring(0, 11);
                var q = (-(qty.Value)).ToString();

                portfolioContractsQty.Add(q);
                portfolioContracts.Add(c);

                string temp = "( " + q + " * " + c + " )";
                equation += temp;
                portfolioEquation.Text = equation;
            }

        }

        async private void Analyze_Button_Clicked(object sender, EventArgs e)
        {
            //DisplayAlert("warning", "you entered th anlayze button event handler","ok");
            
                try
                {
                    mainchart.Series.Clear();

                    List<Dictionary<string, string>> portfoliojson = new List<Dictionary<string, string>>();

                    for (var i = 0; i < portfolioContracts.Count; i++)
                    {
                        Dictionary<string, string> temp = new Dictionary<string, string>();

                        temp.Add("Qty", portfolioContractsQty[i].ToString());
                        temp.Add("contract", portfolioContracts[i].ToString());

                        portfoliojson.Add(temp);
                    }

                    //------------------------------------Graph data--------------------------------------------------------
                    string graphtype1 = null;

                    if (graphtype.SelectedItem.ToString() == "Portfolio vs Front Outright")
                    {
                        graphtype1 = "PortfolioVsOutright";
                    }
                    else if (graphtype.SelectedItem.ToString() == "5 day change in Portfolio vs 5 day change in Outright")
                    {
                        graphtype1 = "5DayChange";
                    }
                    else if (graphtype.SelectedItem.ToString() == "10 day change in Portfolio vs 10 day change in Outright")
                    {
                        graphtype1 = "10DayChange";
                    }

                    var value = new test_COApp.CPDataPostJson()
                    {
                        asOnDate = asOnDate.Date.ToString("yyyy-MM-dd"),
                        portfolio = portfoliojson,
                        lookback = lookbackWindow.SelectedItem.ToString(),
                        graphType = graphtype1
                    };

                    var client = new HttpClient();
                    client.BaseAddress = new Uri("http://ec2-3-94-188-74.compute-1.amazonaws.com:5000");
                    string jsondata = JsonConvert.SerializeObject(value);
                    var content = new StringContent(jsondata, Encoding.UTF8, "application/json");
                     

                    HttpResponseMessage response = await client.PostAsync("/portfoliodata", content);
                    var json = await response.Content.ReadAsStringAsync();
                    Debug.WriteLine("json ---->" + json);
                    Debug.WriteLine("json string ---->" + json.ToString());
                    var regressionDataCollection = JsonConvert.DeserializeObject<CPRegressioDetailsViewModel>(json);
                    Debug.WriteLine("regression data deserialized");
                    var graphDataCollection = JsonConvert.DeserializeObject<CPgraphDataViewModel>(json);

                    regression.Text = "Equation : " + regressionDataCollection.RegressionDetails.ElementAt(0).equation;
                    polyregression.Text = "Equation : " + regressionDataCollection.RegressionDetails.ElementAt(0).PolyEquation;
                    r2regression.Text = "R^2 = " + regressionDataCollection.RegressionDetails.ElementAt(0).rsquared;
                    r2polyregression.Text = "R^2 = " + regressionDataCollection.RegressionDetails.ElementAt(0).rsquaredPoly;

                    ScatterSeries scatterSeries = new ScatterSeries()
                    {
                        ItemsSource = graphDataCollection.GraphData,
                        XBindingPath = "Chartx",
                        YBindingPath = "Scattery",
                        EnableAnimation = true,
                        StrokeColor = Color.Orange,
                        StrokeWidth = 2
                    };
                    mainchart.Series.Add(scatterSeries);

                    SplineSeries splineSeriesreg = new SplineSeries()
                    {
                        ItemsSource = graphDataCollection.GraphData,
                        XBindingPath = "Chartx",
                        YBindingPath = "Regressiony",
                        Color = Color.Blue,
                        EnableAnimation = true
                    };

                    mainchart.Series.Add(splineSeriesreg);

                    SplineSeries polysplineSeries = new SplineSeries()
                    {
                        ItemsSource = graphDataCollection.GraphData,
                        XBindingPath = "Chartx",
                        YBindingPath = "PolyRegressiony",
                        Color = Color.LightBlue,
                        EnableAnimation = true
                    };

                    mainchart.Series.Add(polysplineSeries);
                }
                catch (Exception r)
                {
                    Debug.WriteLine(r);
                }
                finally
                {
                    Debug.WriteLine("ran successfully!");
                }
            
            
        }

        private void Reset_Button_Clicked(object sender, EventArgs e)
        {
            populateContract.Clear();
            equation = "";
            portfolioContracts.Clear();
            portfolioContractsQty.Clear();

            lookbackWindow.SelectedIndex = 0;
            asOnDate.Date = DateTime.Today.Date;
            pickContract.ItemsSource = null;
            pickContract.IsEnabled = false;
            buySell.SelectedIndex = 0;
            portfolioEquation.Text = equation;
            graphtype.SelectedIndex = 0;

            regression.Text = "";
            r2regression.Text = "";
            polyregression.Text = "";
            r2polyregression.Text = "";

            mainchart.Series.Clear();

        }

    }
}