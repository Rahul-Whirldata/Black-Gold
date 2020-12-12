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
        List<string> portfolioContracts = new List<string>();
        List<string> portfolioContractsQty = new List<string>();
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
            client.BaseAddress = new Uri("http://192.168.1.3:5000");

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
                    portfolioContractsQty.Add("1");
                    portfolioContracts.Add(pickContract.SelectedItem.ToString().Substring(0, 11));
                    fly.ItemsSource = null;
                    fly.ItemsSource = portfolioContracts;
                    fly.SelectedIndex = 1;
                    Debug.WriteLine("buy added");
                    foreach (var i in portfolioContracts)
                    {
                        Debug.WriteLine(i);
                    }
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
                    portfolioContractsQty.Add("-1");
                    portfolioContracts.Add(pickContract.SelectedItem.ToString().Substring(0, 11));
                    fly.ItemsSource = null;
                    fly.ItemsSource = portfolioContracts;
                    fly.SelectedIndex = 1;
                    Debug.WriteLine("sell added");
                    foreach (var i in portfolioContracts)
                    {
                        Debug.WriteLine(i);
                    }
                }
                else
                {
                    await DisplayAlert("Warning", "You have already sold one contract", "OK");
                }
            }
                                    
        }

        async private void Analyze_Button_Clicked(object sender, EventArgs e)
        {
            try
            {
                mainchart1.Series.Clear();
                mainchart2.Series.Clear();

                List<Dictionary<string, string>> portfoliojson = new List<Dictionary<string, string>>();
                Dictionary<string, string> temp = new Dictionary<string, string>();

                temp.Add("Qty", portfolioContractsQty[0].ToString());
                temp.Add("contract", portfolioContracts[0].ToString());

                portfoliojson.Add(temp);

                Dictionary<string, string> temp1 = new Dictionary<string, string>();

                temp1.Add("Qty", portfolioContractsQty[1].ToString());
                temp1.Add("contract", portfolioContracts[1].ToString());

                portfoliojson.Add(temp1);
                //------------------------------------Graph data 1--------------------------------------------------------
                string graphtype = null;

                if (graphtype1.SelectedItem.ToString() == "Combined vs Outright")
                {
                    graphtype = "CombinedVsOutright";
                }
                else if (graphtype1.SelectedItem.ToString() == "Fly vs Fly Regression")
                {
                    graphtype = "flyRegressionPoly";
                }
                else if (graphtype1.SelectedItem.ToString() == "Fly vs Average Outright")
                {
                    graphtype = "FlyVsAvgOutright";
                }
                else if (graphtype1.SelectedItem.ToString() == "Fly vs Outright")
                {
                    graphtype = "FlyVsOutright";
                }

                var value = new test_COApp.AnalyzeDataPostJson()
                {
                    asOnDate = asOnDate.Date.ToString("yyyy-MM-dd"),
                    portfolio = portfoliojson,
                    lookback = lookbackWindow.SelectedItem.ToString(),
                    graphType = graphtype,
                    fly = fly.SelectedItem.ToString()
                };

                var client = new HttpClient();
                client.BaseAddress = new Uri("http://192.168.1.3:5000");
                string jsondata = JsonConvert.SerializeObject(value);
                var content = new StringContent(jsondata, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await client.PostAsync("/analyzegraphdata", content);
                var json = await response.Content.ReadAsStringAsync();
                var regressionDataCollection = JsonConvert.DeserializeObject<RegressioDetailsViewModel>(json);
                Debug.WriteLine("regression data deserialized");
                var graphDataCollection = JsonConvert.DeserializeObject<graphDataViewModel>(json);

                regression.Text = regressionDataCollection.RegressionDetails.ElementAt(0).equation;
                polyregression.Text = regressionDataCollection.RegressionDetails.ElementAt(0).PolyEquation;

                ScatterSeries scatterSeries = new ScatterSeries()
                {
                    ItemsSource = graphDataCollection.GraphData,
                    XBindingPath = "Chartx",
                    YBindingPath = "Scattery",
                    EnableAnimation = true,
                    StrokeColor = Color.Orange,
                    StrokeWidth = 2
                };
                mainchart1.Series.Add(scatterSeries);

                SplineSeries splineSeriesreg = new SplineSeries()
                {
                    ItemsSource = graphDataCollection.GraphData,
                    XBindingPath = "Chartx",
                    YBindingPath = "Regressiony",
                    Color = Color.Blue,
                    EnableAnimation = true
                };

                mainchart1.Series.Add(splineSeriesreg);

                SplineSeries polysplineSeries = new SplineSeries()
                {
                    ItemsSource = graphDataCollection.GraphData,
                    XBindingPath = "Chartx",
                    YBindingPath = "PolyRegressiony",
                    Color = Color.LightBlue,
                    EnableAnimation = true
                };

                mainchart1.Series.Add(polysplineSeries);
                //-----------------------------------------------------graph data 2------------------------------------------
                string graphtype0 = null;

                if (graphtype2.SelectedItem.ToString() == "Combined vs Outright")
                {
                    graphtype0 = "CombinedVsOutright";
                }
                else if (graphtype2.SelectedItem.ToString() == "Fly vs Fly Regression")
                {
                    graphtype0 = "flyRegressionPoly";
                }
                else if (graphtype2.SelectedItem.ToString() == "Fly vs Average Outright")
                {
                    graphtype0 = "FlyVsAvgOutright";
                }
                else if (graphtype2.SelectedItem.ToString() == "Fly vs Outright")
                {
                    graphtype0 = "FlyVsOutright";
                }
                
                var value1 = new test_COApp.AnalyzeDataPostJson()
                {
                    asOnDate = asOnDate.Date.ToString("yyyy-MM-dd"),
                    portfolio = portfoliojson,
                    lookback = lookbackWindow.SelectedItem.ToString(),
                    graphType = graphtype0,
                    fly = fly.SelectedItem.ToString()
                };
                
                var client1 = new HttpClient();
                client1.BaseAddress = new Uri("http://192.168.1.3:5000");
                string jsondata1 = JsonConvert.SerializeObject(value1);
                var content1 = new StringContent(jsondata1, Encoding.UTF8, "application/json");
                HttpResponseMessage response1 = await client1.PostAsync("/analyzegraphdata", content1);
                var json1 = await response1.Content.ReadAsStringAsync();
                var regressionDataCollection1 = JsonConvert.DeserializeObject<RegressioDetailsViewModel>(json1);
                Debug.WriteLine("regression data deserialized");
                var graphDataCollection1 = JsonConvert.DeserializeObject<graphDataViewModel>(json1);

                regression1.Text = regressionDataCollection1.RegressionDetails.ElementAt(0).equation;
                polyregression1.Text = regressionDataCollection1.RegressionDetails.ElementAt(0).PolyEquation;

                ScatterSeries scatterSeries1 = new ScatterSeries()
                {
                    ItemsSource = graphDataCollection1.GraphData,
                    XBindingPath = "Chartx",
                    YBindingPath = "Scattery",
                    EnableAnimation = true,
                    StrokeColor = Color.Orange,
                    StrokeWidth = 2
                };
                mainchart2.Series.Add(scatterSeries1);

                SplineSeries splineSeriesreg1 = new SplineSeries()
                {
                    ItemsSource = graphDataCollection1.GraphData,
                    XBindingPath = "Chartx",
                    YBindingPath = "Regressiony",
                    Color = Color.Blue,
                    EnableAnimation = true
                };

                mainchart2.Series.Add(splineSeriesreg1);

                SplineSeries polysplineSeries1 = new SplineSeries()
                {
                    ItemsSource = graphDataCollection1.GraphData,
                    XBindingPath = "Chartx",
                    YBindingPath = "PolyRegressiony",
                    Color = Color.LightBlue,
                    EnableAnimation = true
                };

                mainchart2.Series.Add(polysplineSeries1);
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
            buyCount = 0;
            sellCount = 0;
            portfolioContracts.Clear();
            portfolioContractsQty.Clear();

            lookbackWindow.SelectedIndex = 0;
            asOnDate.Date = DateTime.Today.Date;
            pickContract.ItemsSource = null;
            pickContract.IsEnabled = false;
            buySell.SelectedIndex = 0;
            portfolioEquation.Text = equation;
            graphtype1.SelectedIndex = 0;
            fly.ItemsSource = null;
            graphtype2.SelectedIndex = 0;

            regression.Text = "";
            regression1.Text = "";
            polyregression.Text = "";
            polyregression1.Text = "";

            mainchart1.Series.Clear();
            mainchart2.Series.Clear();

        }

        //async private void graphtype1_SelectedIndexChanged(object sender, EventArgs e)
        //{
        //    mainchart1.Series.Clear();

        //    List<Dictionary<string, string>> portfoliojson = new List<Dictionary<string, string>>();
        //    Dictionary<string, string> temp = new Dictionary<string, string>();

        //    temp.Add("Qty", portfolioContractsQty[0].ToString());
        //    temp.Add("contract", portfolioContracts[0].ToString());

        //    portfoliojson.Add(temp);

        //    Dictionary<string, string> temp1 = new Dictionary<string, string>();

        //    temp1.Add("Qty", portfolioContractsQty[1].ToString());
        //    temp1.Add("contract", portfolioContracts[1].ToString());

        //    portfoliojson.Add(temp1);
        //    //---------------------------------------graph data 1---------------------------------------------
        //    string graphtype = null;

        //    if (graphtype1.SelectedItem.ToString() == "Combined vs Outright")
        //    {
        //        graphtype = "CombinedVsOutright";
        //    }
        //    else if (graphtype1.SelectedItem.ToString() == "Fly vs Fly Regression")
        //    {
        //        graphtype = "flyRegressionPoly";
        //    }
        //    else if (graphtype1.SelectedItem.ToString() == "Fly vs Average Outright")
        //    {
        //        graphtype = "FlyVsAvgOutright";
        //    }
        //    else if (graphtype1.SelectedItem.ToString() == "Fly vs Outright")
        //    {
        //        graphtype = "FlyVsOutright";
        //    }

        //    var value = new AnalyzeDataPostJson()
        //    {
        //        asOnDate = asOnDate.Date.ToString("yyyy-MM-dd"),
        //        portfolio = portfoliojson,
        //        lookback = lookbackWindow.SelectedItem.ToString(),
        //        graphType = graphtype,
        //        fly = fly.SelectedItem.ToString()
        //    };

        //    var client = new HttpClient();
        //    client.BaseAddress = new Uri("http://192.168.1.3:5000");
        //    string jsondata = JsonConvert.SerializeObject(value);
        //    var content = new StringContent(jsondata, Encoding.UTF8, "application/json");
        //    HttpResponseMessage response = await client.PostAsync("/analyzegraphdata", content);
        //    var json = await response.Content.ReadAsStringAsync();
        //    var regressionDataCollection = JsonConvert.DeserializeObject<RegressioDetailsViewModel>(json);
        //    Debug.WriteLine("regression data deserialized");
        //    var graphDataCollection = JsonConvert.DeserializeObject<graphDataViewModel>(json);

        //    regression.Text = regressionDataCollection.RegressionDetails.ElementAt(0).equation;
        //    polyregression.Text = regressionDataCollection.RegressionDetails.ElementAt(0).PolyEquation;

        //    ScatterSeries scatterSeries = new ScatterSeries()
        //    {
        //        ItemsSource = graphDataCollection.GraphData,
        //        XBindingPath = "Chartx",
        //        YBindingPath = "Scattery",
        //        EnableAnimation = true,
        //        Color = Color.Orange,
        //    };
        //    mainchart1.Series.Add(scatterSeries);

        //    SplineSeries splineSeriesreg = new SplineSeries()
        //    {
        //        ItemsSource = graphDataCollection.GraphData,
        //        XBindingPath = "Chartx",
        //        YBindingPath = "Regressiony",
        //        Color = Color.Blue,
        //        EnableAnimation = true
        //    };

        //    mainchart1.Series.Add(splineSeriesreg);

        //    SplineSeries polysplineSeries = new SplineSeries()
        //    {
        //        ItemsSource = graphDataCollection.GraphData,
        //        XBindingPath = "Chartx",
        //        YBindingPath = "PolyRegressiony",
        //        Color = Color.LightBlue,
        //        EnableAnimation = true
        //    };

        //    mainchart1.Series.Add(polysplineSeries);
        //}

        //async private void fly_SelectedIndexChanged(object sender, EventArgs e)
        //{
        //    mainchart2.Series.Clear();

        //    List<Dictionary<string, string>> portfoliojson = new List<Dictionary<string, string>>();
        //    Dictionary<string, string> temp = new Dictionary<string, string>();

        //    temp.Add("Qty", portfolioContractsQty[0].ToString());
        //    temp.Add("contract", portfolioContracts[0].ToString());

        //    portfoliojson.Add(temp);

        //    Dictionary<string, string> temp1 = new Dictionary<string, string>();

        //    temp1.Add("Qty", portfolioContractsQty[1].ToString());
        //    temp1.Add("contract", portfolioContracts[1].ToString());

        //    portfoliojson.Add(temp1);
        //    //-----------------------------------------------------graph data 2------------------------------------------
        //    string graphtype0 = null;

        //    if (graphtype2.SelectedItem.ToString() == "Combined vs Outright")
        //    {
        //        graphtype0 = "CombinedVsOutright";
        //    }
        //    else if (graphtype2.SelectedItem.ToString() == "Fly vs Fly Regression")
        //    {
        //        graphtype0 = "flyRegressionPoly";
        //    }
        //    else if (graphtype2.SelectedItem.ToString() == "Fly vs Average Outright")
        //    {
        //        graphtype0 = "FlyVsAvgOutright";
        //    }
        //    else if (graphtype2.SelectedItem.ToString() == "Fly vs Outright")
        //    {
        //        graphtype0 = "FlyVsOutright";
        //    }
        //    var value1 = new AnalyzeDataPostJson()
        //    {
        //        asOnDate = asOnDate.Date.ToString("yyyy-MM-dd"),
        //        portfolio = portfoliojson,
        //        lookback = lookbackWindow.SelectedItem.ToString(),
        //        graphType = graphtype0,
        //        fly = fly.SelectedItem.ToString()
        //    };

        //    var client1 = new HttpClient();
        //    client1.BaseAddress = new Uri("http://192.168.1.3:5000");
        //    string jsondata1 = JsonConvert.SerializeObject(value1);
        //    var content1 = new StringContent(jsondata1, Encoding.UTF8, "application/json");
        //    HttpResponseMessage response1 = await client1.PostAsync("/analyzegraphdata", content1);
        //    var json1 = await response1.Content.ReadAsStringAsync();
        //    var regressionDataCollection1 = JsonConvert.DeserializeObject<RegressioDetailsViewModel>(json1);
        //    Debug.WriteLine("regression data deserialized");
        //    var graphDataCollection1 = JsonConvert.DeserializeObject<graphDataViewModel>(json1);

        //    regression1.Text = regressionDataCollection1.RegressionDetails.ElementAt(0).equation;
        //    polyregression1.Text = regressionDataCollection1.RegressionDetails.ElementAt(0).PolyEquation;

        //    ScatterSeries scatterSeries1 = new ScatterSeries()
        //    {
        //        ItemsSource = graphDataCollection1.GraphData,
        //        XBindingPath = "Chartx",
        //        YBindingPath = "Scattery",
        //        EnableAnimation = true,
        //        Color = Color.Orange,
        //    };
        //    mainchart2.Series.Add(scatterSeries1);

        //    SplineSeries splineSeriesreg1 = new SplineSeries()
        //    {
        //        ItemsSource = graphDataCollection1.GraphData,
        //        XBindingPath = "Chartx",
        //        YBindingPath = "Regressiony",
        //        Color = Color.Blue,
        //        EnableAnimation = true
        //    };

        //    mainchart2.Series.Add(splineSeriesreg1);

        //    SplineSeries polysplineSeries1 = new SplineSeries()
        //    {
        //        ItemsSource = graphDataCollection1.GraphData,
        //        XBindingPath = "Chartx",
        //        YBindingPath = "PolyRegressiony",
        //        Color = Color.LightBlue,
        //        EnableAnimation = true
        //    };

        //    mainchart2.Series.Add(polysplineSeries1);
        //}

        //async private void graphtype2_SelectedIndexChanged(object sender, EventArgs e)
        //{
        //    mainchart2.Series.Clear();

        //    List<Dictionary<string, string>> portfoliojson = new List<Dictionary<string, string>>();
        //    Dictionary<string, string> temp = new Dictionary<string, string>();

        //    temp.Add("Qty", portfolioContractsQty[0].ToString());
        //    temp.Add("contract", portfolioContracts[0].ToString());

        //    portfoliojson.Add(temp);

        //    Dictionary<string, string> temp1 = new Dictionary<string, string>();

        //    temp1.Add("Qty", portfolioContractsQty[1].ToString());
        //    temp1.Add("contract", portfolioContracts[1].ToString());

        //    portfoliojson.Add(temp1);
        //    //-----------------------------------------------------graph data 2------------------------------------------
        //    string graphtype0 = null;

        //    if (graphtype2.SelectedItem.ToString() == "Combined vs Outright")
        //    {
        //        graphtype0 = "CombinedVsOutright";
        //    }
        //    else if (graphtype2.SelectedItem.ToString() == "Fly vs Fly Regression")
        //    {
        //        graphtype0 = "flyRegressionPoly";
        //    }
        //    else if (graphtype2.SelectedItem.ToString() == "Fly vs Average Outright")
        //    {
        //        graphtype0 = "FlyVsAvgOutright";
        //    }
        //    else if (graphtype2.SelectedItem.ToString() == "Fly vs Outright")
        //    {
        //        graphtype0 = "FlyVsOutright";
        //    }
        //    var value1 = new AnalyzeDataPostJson()
        //    {
        //        asOnDate = asOnDate.Date.ToString("yyyy-MM-dd"),
        //        portfolio = portfoliojson,
        //        lookback = lookbackWindow.SelectedItem.ToString(),
        //        graphType = graphtype0,
        //        fly = fly.SelectedItem.ToString()
        //    };

        //    var client1 = new HttpClient();
        //    client1.BaseAddress = new Uri("http://192.168.1.3:5000");
        //    string jsondata1 = JsonConvert.SerializeObject(value1);
        //    var content1 = new StringContent(jsondata1, Encoding.UTF8, "application/json");
        //    HttpResponseMessage response1 = await client1.PostAsync("/analyzegraphdata", content1);
        //    var json1 = await response1.Content.ReadAsStringAsync();
        //    var regressionDataCollection1 = JsonConvert.DeserializeObject<RegressioDetailsViewModel>(json1);
        //    Debug.WriteLine("regression data deserialized");
        //    var graphDataCollection1 = JsonConvert.DeserializeObject<graphDataViewModel>(json1);

        //    regression1.Text = regressionDataCollection1.RegressionDetails.ElementAt(0).equation;
        //    polyregression1.Text = regressionDataCollection1.RegressionDetails.ElementAt(0).PolyEquation;

        //    ScatterSeries scatterSeries1 = new ScatterSeries()
        //    {
        //        ItemsSource = graphDataCollection1.GraphData,
        //        XBindingPath = "Chartx",
        //        YBindingPath = "Scattery",
        //        EnableAnimation = true,
        //        Color = Color.Orange,
        //    };
        //    mainchart2.Series.Add(scatterSeries1);

        //    SplineSeries splineSeriesreg1 = new SplineSeries()
        //    {
        //        ItemsSource = graphDataCollection1.GraphData,
        //        XBindingPath = "Chartx",
        //        YBindingPath = "Regressiony",
        //        Color = Color.Blue,
        //        EnableAnimation = true
        //    };

        //    mainchart2.Series.Add(splineSeriesreg1);

        //    SplineSeries polysplineSeries1 = new SplineSeries()
        //    {
        //        ItemsSource = graphDataCollection1.GraphData,
        //        XBindingPath = "Chartx",
        //        YBindingPath = "PolyRegressiony",
        //        Color = Color.LightBlue,
        //        EnableAnimation = true
        //    };

        //    mainchart2.Series.Add(polysplineSeries1);
        //}
    }
}