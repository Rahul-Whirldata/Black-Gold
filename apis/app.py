# import app_utils
from flask_cors import CORS
from flask import Flask, session, g, request, send_file, jsonify
from flask_restful import Resource, reqparse
import os
import pandas as pd
import json
import logging
import datetime
import dateutil.relativedelta
from model.polyregression import getPolyRegressionLine

app = Flask(__name__)
cors = CORS(app)

HEADERS = {"Access-Control-Allow-Methods": "*",
           "Access-Control-Allow-Headers": "*"}

    

@app.route('/ping', methods=['GET'])
def ping():
    return "pong"


@app.route("/forwardcurves", methods=['POST', 'OPTIONS'])
def forwardcurves():

    data = request.json
    

    dataTypeFullName = {'O': 'Outright', 'S': 'Spread', 'F': 'Fly'}

    baseDate = data['base_date']
    compareDates = data['compare_dates']
    lookForward = int(data["lookForward"])
    dataType = data["type"]

    if (len(compareDates) > 5):
        return {
            "message": {
                "compare_dates": "Cannot be more than 5"
            }
        }
    if dataType in ["S", "F"]:
        monthOfSpread = int(data["monthOf"])
        title = "{}M {}".format(monthOfSpread, dataTypeFullName[dataType])
    else:
        monthOfSpread = 0
        title = dataTypeFullName[dataType]

    XLabel = ''
    contractCode = 'CL'
    YLegends = []
    YValues = []
    Month_Name = {'F': 1, 'G': 2, 'H': 3, 'J': 4, 'K': 5,
                    'M': 6, 'N': 7, 'Q': 8, 'U': 9, 'V': 10, 'X': 11, 'Z': 12}

    try:

        master = pd.read_csv(r"./master.csv",
                                low_memory=False)
    except Exception as e:
        return {
            'statusCode': 410,
            'data': '',
            'message': str(e)
        }

    try:

        master = master.query(
            " contract_category == @contractCode and settle != -1 and contract_date >= '2018-08-06' ")

        compareDates.append(baseDate)
        YLegends = compareDates
        a = master[(master['contract_date'].isin(compareDates))]
        a = a[(a['contract_type'] == dataType) & (
            a['month_of_spread'] == monthOfSpread)]
        result = a.pivot(index='contract_label',
                            columns='contract_date', values='settle')
        result = result.reset_index()
        result.rename(columns={'contract_label': 'category',
                                baseDate: 'settle0'}, inplace=True)
        cols_list=list(result.columns)
        compare_dt={}
        count=0
        for ind,name in enumerate(cols_list):
            if name!='category' and name!='settle0':
                count=count+1
                compare_dt["settle"+str(count)]=name
        for key,value in compare_dt.items():
            result.rename(columns={value:key},inplace=True)
            result[key+"_desc"]=value
        count=0
        cols_list=list(result.columns)
        for ind,name in enumerate(cols_list):
            if name!='category' and name!='settle0':
                count=count+1
        print(count)
        number_of_settles=count//2
        new_cols_list=['category','settle0']
        for i in range(number_of_settles):
            print(i)
            new_cols_list.append("settle"+str(i+1))
            new_cols_list.append("settle"+str(i+1)+"_desc")
            
        result = result.assign(year=result['category'].str[-2:],
                                month=result['category'].str[:1])
        result['Month'] = result['month'].apply(
            lambda x: Month_Name.get(x))
        result = result.sort_values(['year', 'Month'])
        # To implement LookForward Line 1
        year = int(result['year'].iloc[0])
        newYear = year + lookForward  # To implement LookForward Line2
        # To implement LookFoward Line 3
        result = result[(result['year']).map(int) <= newYear]
        result.drop(columns=['year', 'month', 'Month'], inplace=True)
        YValues = result.columns.tolist()
        YValues.pop(0)
        # print(a.head().to_string())
        if dataType == 'O':

            XLabel = 'Outrights'

        elif dataType == 'S':

            XLabel = 'Spreads'

        elif dataType == 'F':

            XLabel = 'Flys'

        result.dropna(axis=0, inplace=True)
        result=result[new_cols_list]
        df = result.to_dict('records')
        # df = json.dumps(df).replace("NaN", "null")
        df = json.dumps(df).replace('"', "'")

        print("worked!!!")
        return df

    except Exception as e:
        logging.error('error occurred : ' + str(e))

@app.route("/activeflylist", methods=['POST', 'OPTIONS'])
def activeflylist():
    parser = reqparse.RequestParser()
    parser.add_argument('asOnDate', help='Enter As on Date', required=True)

    args = parser.parse_args()

    try:
        date_object = pd.to_datetime(args['asOnDate'])

    except Exception as e:
        return {
            'statusCode': 410,
            'data': '',
            'message': str(logging.error("Exception Occurred", exc_info=True))
        }

    try:

        master = pd.read_csv(r"./master.csv", low_memory=False)

        contractCode = 'CL'

        master = master.query(f"contract_category=='CL' and contract_type=='F' and contract_date=='{args['asOnDate']}'")[
            ["contract_label", "contract_key"]]
        master.rename(columns={"contract_label": "key",
                                "contract_key": "value"}, inplace=True)
        master.drop(columns=["key"], inplace=True)

        # temp = master.value.tolist()
        # df = {"data": temp}
        df = master.to_dict('records')
        
        df = json.dumps(df).replace('"', "'")

        return df

    except Exception as e:
        return {
            'statusCode': 405,
            'data': '',
            'message': str(logging.error("Exception Occurred", exc_info=True))
        }

@app.route("/analyzegraphdata", methods=['POST', 'OPTIONS'])
def analyzegraphdata():
    pd.options.mode.chained_assignment = None

    parser = reqparse.RequestParser()

    parser.add_argument('asOnDate', help='Enter As on Date', required=True)
    parser.add_argument('portfolio', help='Portfolio data required',
                        action="append", default=list, required=True)
    parser.add_argument('lookback', help='Bad Choice: {error_msg}',
                        choices=("1", "3", "6", "9", "12", "All"), required=True)
    parser.add_argument('graphType', help='Bad Choice: {error_msg}',
                        choices=('CombinedVsOutright', 'CombinedVsWings', 'flyRegressionPoly', 'movingAverage', 'combinedGraph',
                                'FlyVsOutright', 'FlyVsWings', 'FlyVsAvgOutright'), required=True)
    parser.add_argument('fly', help='Fly value is required', required=True)

    args = parser.parse_args()

    try:

        asOnDate = datetime.datetime.strptime(args['asOnDate'], "%Y-%m-%d")
        portfolio = [eval(i) for i in args['portfolio']]
        lookback = args['lookback']
        graphType = args['graphType']
        selectedFly = args['fly']

        # Common instanstiation
        contractCode = 'CL'
        firstFly = ''
        secondFly = ''
        lookback_date = asOnDate - dateutil.relativedelta.relativedelta(
            months=int(lookback)) if lookback in ["1", "3", "6", "9", "12"] else 'All'

        curatedData = pd.read_csv(
            r"./master.csv", low_memory=False, parse_dates=["contract_date"])

        if lookback_date != 'All':

            curatedData = curatedData.query(
                " contract_category == @contractCode and contract_date >= @lookback_date and contract_date <= @asOnDate ")
        else:
            curatedData = curatedData.query(
                " contract_category == @contractCode and contract_date >= '2018-08-06' and contract_date <= @asOnDate ")

        curatedData = curatedData[[
            'contract_date', 'contract_label', 'settle']]

        # Extracting the first & second flys sequentially
        # dict = { 'Qty' : '1', 'contract' : 'A20/B20/C20' }
        for dic in portfolio:
            firstFly = dic['contract'] if dic['Qty'] == "1" else firstFly
            secondFly = dic['contract'] if dic['Qty'] == "-1" else secondFly

        # Calculating other necessary columns (all possibilities)
        firstOutrightFirstFly = firstFly.split('/')[0]
        secondOutrightSecondFly = selectedFly.split('/')[-1]

        chosenOutright = selectedFly.split('/')[0]
        secondOurightChosenFly = selectedFly.split('/')[1]
        thirdOurightChosenFly = selectedFly.split('/')[-1]

        # Filtering for the input Flys
        contracts = [firstFly, secondFly, firstOutrightFirstFly, chosenOutright,
                        secondOutrightSecondFly, secondOurightChosenFly, thirdOurightChosenFly]

        data = curatedData.loc[curatedData['contract_label'].isin(
            contracts)]
        data = data.pivot(index='contract_date',
                            columns='contract_label', values='settle')
        data = data.reset_index()

        # Calculating the combined value
        wingName = firstOutrightFirstFly + '/' + secondOutrightSecondFly
        chosenWingName = chosenOutright + '/' + thirdOurightChosenFly

        data = data.assign(combined=lambda x: x[firstFly] - x[secondFly],
                            wingName=lambda x: x[firstOutrightFirstFly] -
                            x[secondOutrightSecondFly],
                            wingName1=lambda x: x[chosenOutright] -
                            x[thirdOurightChosenFly],
                            Avg=lambda x: (x[chosenOutright] + x[secondOurightChosenFly] + x[thirdOurightChosenFly]) / 3)

        data.rename(columns={'contract_date': 'date', 'wingName': wingName,
                                'wingName1': chosenWingName}, inplace=True)
        data['date'] = data['date'].dt.strftime("%Y-%m-%d")

        # print(data.head().to_string()) # DataFrame of [date, firstFly, secondFly, combined]

        # Filterting on graphType
        def CombinedVsOutright(data):
            data = data[['date', 'combined', firstOutrightFirstFly]]

            data.rename(columns={'combined': 'Scatter'}, inplace=True)

            # dataJson = json.dumps(data.to_dict(
            #     'records')).replace('NaN', 'null')

            data, rsquared, rsquaredPoly, equation, polyEquation, standardError, standardErrorPoly = getPolyRegressionLine(
                data)
            returnString = {
                'RegressionDetails': [{
                    "equation": equation,
                    "PolyEquation": polyEquation,
                    "rsquared": rsquared,
                    "rsquaredPoly": rsquaredPoly,
                    "standardError": standardError,
                    "standardErrorPoly": standardErrorPoly,
                }],
                "GraphData": data
            }
            temp = json.dumps(returnString)
            returnString = temp.replace('"', "'")
            returnString = returnString.replace("'[", "[")
            returnString = returnString.replace("]'", "]")
            return returnString

        def CombinedVsWings(data):
            data = data[['date', 'combined', wingName]]

            data.rename(columns={'combined': 'Scatter'}, inplace=True)

            # dataJson = json.dumps(data.to_dict(
            #     'records')).replace('NaN', 'null')

            data, rsquared, rsquaredPoly, equation, polyEquation, standardError, standardErrorPoly = getPolyRegressionLine(
                data)

            return {
                'statusCode': 200,
                'title': '',
                'X-axis': {
                    'label': wingName,
                    'type': 'value',
                    'legends': wingName,
                    'value': wingName
                },
                'Y-axis': {
                    'label': 'Combined',
                    'type': 'value',
                    'legends': ['Combined', 'Linear Regression', 'Polynomial Regression'],
                    'value': ['Scatter', 'Regression', 'polyRegression']
                },
                'RegressionDetails': {
                    "equation": equation,
                    "PolyEquation": polyEquation,
                    "rsquared": rsquared,
                    "rsquaredPoly": rsquaredPoly,
                    "standardError": standardError,
                    "standardErrorPoly": standardErrorPoly,
                },
                "data": data
            }
        
        def flyRegressionPoly(data):
            data = data[['date', firstFly, secondFly]]

            data.rename(columns={firstFly: 'Scatter'}, inplace=True)

            # dataJson = json.dumps(data.to_dict(
            #     'records')).replace('NaN', 'null')

            data, rsquared, rsquaredPoly, equation, polyEquation, standardError, standardErrorPoly = getPolyRegressionLine(
                data)
            returnString = {
                'RegressionDetails': [{
                    "equation": equation,
                    "PolyEquation": polyEquation,
                    "rsquared": rsquared,
                    "rsquaredPoly": rsquaredPoly,
                    "standardError": standardError,
                    "standardErrorPoly": standardErrorPoly,
                }],
                "GraphData": data
            }
            temp = json.dumps(returnString)
            returnString = temp.replace('"', "'")
            returnString = returnString.replace("'[", "[")
            returnString = returnString.replace("]'", "]")
            return returnString

        def movingAverage(data):
            data = data[['date', 'combined']]

            for window in [5, 20]:
                data.loc[:, 'combined_' +
                            str(window)] = data.loc[:, 'combined'].rolling(window).mean()

            data.sort_values(['date'])

            return {
                'statusCode': 200,
                'title': '',
                'X-axis': {
                    'label': 'date',
                    'type': 'date',
                    'legends': 'date',
                    'value': 'date'
                },
                'Y-axis': {
                    'label': 'Combined',
                    'type': 'value',
                    'legends': ['Combined', 'Combined_5', 'Combined_20'],
                    'value': ['combined', 'combined_5', 'combined_20']
                },
                'RegressionDetails': {
                    "equation": '',
                    "PolyEquation": '',
                    "rsquared": '',
                    "rsquaredPoly": '',
                    "standardError": '',
                    "standardErrorPoly": '',
                },
                "data": data
            }

        def combinedGraph(data):

            data = data[['date', firstFly, secondFly]]

            return {
                'statusCode': 200,
                'title': '',
                'X-axis': {
                    'label': 'date',
                    'type': 'date',
                    'legends': 'date',
                    'value': 'date'
                },
                'Y-axis': {
                    'label': 'Combined',
                    'type': 'value',
                    'legends': ['Combined', 'Combined_5', 'Combined_20'],
                    'value': ['combined', 'combined_5', 'combined_20']
                },
                'RegressionDetails': {
                    "equation": '',
                    "PolyEquation": '',
                    "rsquared": '',
                    "rsquaredPoly": '',
                    "standardError": '',
                    "standardErrorPoly": '',
                },
                "data": data
            }

        def FlyVsOutright(data):

            data = data[['date', selectedFly, chosenOutright]]

            data.rename(columns={selectedFly: 'Scatter'}, inplace=True)

            # dataJson = json.dumps(data.to_dict(
            #     'records')).replace('NaN', 'null')

            data, rsquared, rsquaredPoly, equation, polyEquation, standardError, standardErrorPoly = getPolyRegressionLine(
                data)
            returnString = {
                'RegressionDetails': [{
                    "equation": equation,
                    "PolyEquation": polyEquation,
                    "rsquared": rsquared,
                    "rsquaredPoly": rsquaredPoly,
                    "standardError": standardError,
                    "standardErrorPoly": standardErrorPoly,
                }],
                "GraphData": data
            }
            temp = json.dumps(returnString)
            returnString = temp.replace('"', "'")
            returnString = returnString.replace("'[", "[")
            returnString = returnString.replace("]'", "]")
            return returnString

        def FlyVsWings(data):

            data = data[['date', selectedFly, chosenWingName]]

            data.rename(columns={selectedFly: 'Scatter'}, inplace=True)

            # dataJson = json.dumps(data.to_dict(
            #     'records')).replace('NaN', 'null')

            data, rsquared, rsquaredPoly, equation, polyEquation, standardError, standardErrorPoly = getPolyRegressionLine(
                data)
            
            return {
                'statusCode': 200,
                'title': '',
                'X-axis': {
                    'label': chosenWingName,
                    'type': 'value',
                    'legends': chosenWingName,
                    'value': chosenWingName
                },
                'Y-axis': {
                    'label': selectedFly,
                    'type': 'value',
                    'legends': [selectedFly, 'Linear Regression', 'Polynomial Regression'],
                    'value': ['Scatter', 'Regression', 'polyRegression']
                },
                'RegressionDetails': {
                    "equation": equation,
                    "PolyEquation": polyEquation,
                    "rsquared": rsquared,
                    "rsquaredPoly": rsquaredPoly,
                    "standardError": standardError,
                    "standardErrorPoly": standardErrorPoly,
                },
                "data": data
            }

        def FlyVsAvgOutright(data):

            data = data[['date', selectedFly, 'Avg']]

            data.rename(columns={selectedFly: 'Scatter'}, inplace=True)

            # dataJson = json.dumps(data.to_dict(
            #     'records')).replace('NaN', 'null')

            data, rsquared, rsquaredPoly, equation, polyEquation, standardError, standardErrorPoly = getPolyRegressionLine(
                data)
            returnString = {
                'RegressionDetails': [{
                    "equation": equation,
                    "PolyEquation": polyEquation,
                    "rsquared": rsquared,
                    "rsquaredPoly": rsquaredPoly,
                    "standardError": standardError,
                    "standardErrorPoly": standardErrorPoly,
                }],
                "GraphData": data
            }
            temp = json.dumps(returnString)
            returnString = temp.replace('"', "'")
            returnString = returnString.replace("'[", "[")
            returnString = returnString.replace("]'", "]")
            return returnString

        graphTypesDict = {
            'CombinedVsOutright': CombinedVsOutright,
            'CombinedVsWings': CombinedVsWings,
            'flyRegressionPoly': flyRegressionPoly,
            'movingAverage': movingAverage,
            'combinedGraph': combinedGraph,
            'FlyVsOutright': FlyVsOutright,
            'FlyVsWings': FlyVsWings,
            'FlyVsAvgOutright': FlyVsAvgOutright
        }

        return graphTypesDict.get(graphType)(data)

    except Exception as e:
        return {
            'statusCode': 430,
            'data': '',
            'message': str(logging.error("Exception Occurred", exc_info=True))
        }

if __name__ == "__main__":
    app.run(debug=True, port=5000, host='0.0.0.0')
