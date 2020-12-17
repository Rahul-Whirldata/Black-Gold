# import app_utils
from flask_cors import CORS
from flask import Flask, session, g, request, send_file, jsonify
from flask_restful import Resource, reqparse
import os
import pandas as pd
import numpy as np
import json
import logging
import datetime
import dateutil.relativedelta
from model.polyregression import getPolyRegressionLine
from sklearn.linear_model import LinearRegression
from itertools import combinations

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
    
    print(data)

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

    print(args)

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

    print(args)

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

@app.route("/portfoliodata", methods=['POST', 'OPTIONS'])
def portfoliodata():
    pd.options.mode.chained_assignment = None
    parser = reqparse.RequestParser()
    parser.add_argument('asOnDate', help='As on Date is mandatory', required=True)
    parser.add_argument('portfolio', help='Portfolio is required', required=True,
                        action="append", default=list)
    parser.add_argument('lookback', help='Lookback period is mandatory', required=True,
                        choices=("1", "3", "6", "9", "12", "All"))
    parser.add_argument('graphType', help='Graph Type is mandatory', required=True,
                        choices=('PortfolioVsOutright', '5DayChange', '10DayChange'))

    args = parser.parse_args()
    print(args)
    try:
        asOnDate = args['asOnDate']
        portfolio = [eval(i) for i in args['portfolio']]
        lookback = args['lookback']
        graphType = args['graphType']

        asOn = datetime.datetime.strptime(asOnDate, "%Y-%m-%d")

        lookback_date = asOn - dateutil.relativedelta.relativedelta(
            months=int(lookback)) if lookback in ["1", "3", "6", "9", "12"] else 'All'

        curatedData = pd.read_csv(
            r"./master.csv", low_memory=False, parse_dates=["contract_date"])

        contractCode = 'CL'
        monthsCode = ['F', 'G', 'H', 'J', 'K',
                        'M', 'N', 'Q', 'U', 'V', 'X', 'Z']
        contractLst = []
        outrightLst = []
        outrightValue = []
        formula = ''

        if lookback_date != 'All':
            curatedData = curatedData.query(
                " contract_category == @contractCode and contract_date >= @lookback_date and contract_date <= @asOn ")
        else:
            curatedData = curatedData.query(
                " contract_category == @contractCode and contract_date >= '2018-08-06' and contract_date <= @asOn ")

        curatedData = curatedData[[
            'contract_date', 'contract_label', 'settle']]

        # Extracting the flys and their quantities, creating necessary formula also
        # dict = { 'Qty' : '1', 'contract' : 'A20/B20/C20' }
        print("Portfolio->", portfolio)
        for dic in portfolio:
            quantity = int(dic['Qty'])

            if quantity > 0:
                quantityText = '+{}'.format(quantity)
            else:
                quantityText = quantity

            formula = formula + \
                "{}*data['{}']".format(quantityText, dic['contract'])
            contractLst.append(dic['contract'])

            outright = dic['contract'].split('/')[0]

            outrightLst.append(outright)

            outrightValue.append(
                int(outright[1:] + "{:02d}".format(monthsCode.index(outright[0]))))

        frontMonth = outrightLst[np.argmin(outrightValue)]

        # Filtering for the input Flys
        data = curatedData.loc[(curatedData['contract_label'].isin(contractLst)) | (
            curatedData['contract_label'].isin(outrightLst))]
        data = data.pivot(index='contract_date',
                            columns='contract_label', values='settle')
        data = data.reset_index()

        # print(data.head().to_string())

        # Calculating the combined value
        data['combined'] = eval(formula)
        data.rename(columns={'contract_date': 'date'}, inplace=True)
        data['date'] = data['date'].dt.strftime("%Y-%m-%d")

        # print(data.head().to_string()) # DataFrame of [date, firstFly, secondFly, combined]

        # Filterting on graphType
        if graphType == 'PortfolioVsOutright':

            data = data[['date', 'combined', frontMonth]]
            data.rename(columns={'combined': 'Scatter'}, inplace=True)
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

        else:

            def change(x):
                return x.iloc[-1] - x.iloc[0]
                # return x[-1] - x[0]

            dayChange = int(
                ''.join(filter(lambda x: x.isdigit(), graphType)))
            columnNames = ['date']

            for col in ['combined', frontMonth]:
                tempVar = '{}day_{}'.format(dayChange, col)
                columnNames.extend([tempVar])
                data[tempVar] = data[col].rolling(
                    window=dayChange).apply(change)

            # Nans will be present as we are taking change in values
            data.dropna(inplace=True)

            data = data[columnNames]
            data.rename(columns={1: 'Scatter'}, inplace=True)
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

    except Exception as e:
        return {
            'statusCode': 410,
            'data': '',
            'message': str(logging.error("Exception Occurred", exc_info=True))
        }

@app.route("/recommendation", methods=['POST', 'OPTIONS'])
def recommendation():

    pd.options.mode.chained_assignment = None

    parser = reqparse.RequestParser()
    parser.add_argument('recommendationDate',
                        help='Recommendation Date is mandatory', required=True)
    parser.add_argument('lookback', help='Lookback Period is mandatory',
                        required=True, choices=("1", "3", "6", "9", "12", "All"))
    
    try:
        # INPUT COLLECTION
        args = parser.parse_args()

        recommendationDate = args['recommendationDate']
        lookback = args['lookback']
        vno = int(1)

        asOn = datetime.datetime.strptime(recommendationDate, "%Y-%m-%d")
        print("Below is the asOn Date...")
        print(asOn)
        print()

        lookback_date = asOn - dateutil.relativedelta.relativedelta(months=int(
            lookback)) - dateutil.relativedelta.relativedelta(days=1) if lookback in ["1", "3", "6", "9", "12"] else 'All'

        # CHOOSING THE SOURCE OF DATA
        if vno == 2:
            curatedData = pd.read_csv(
                r"./Smoothenedmaster.csv", low_memory=False, parse_dates=["contract_date"])
        else:
            curatedData = pd.read_csv(
                r"./master.csv", low_memory=False, parse_dates=["contract_date"])

        contractCode = 'CL'

        # contractLst = [d['contract'] for d in portfolio] # List of flys or contracts
        if vno != 3:
            if lookback_date != 'All':
                curatedData = curatedData.query(
                    " contract_category == @contractCode and contract_date >= @lookback_date and contract_date < @asOn ")
            else:
                curatedData = curatedData.query(
                    " contract_category == @contractCode and contract_date >= '2018-08-06' and contract_date < @asOn ")
        else:
            if lookback_date != 'All':
                curatedData = curatedData.query(
                    " contract_category == @contractCode and contract_date >= @lookback_date and contract_date <= @asOn ")
            else:
                curatedData = curatedData.query(
                    " contract_category == @contractCode and contract_date >= '2018-08-06' and contract_date <= @asOn ")   

        curatedData.drop(
            ['contract_category', 'contract_month', 'contract_year'], axis=1, inplace=True)

        # print(curatedData.tail(10).to_string())

        rdate = curatedData.iloc[-1]['contract_date']
        lastDate = max(curatedData['contract_date']).date()

        # Filtering the necessary Flys from { 1M : 19 Flys, 3M : 8 Flys, 6M : 10 Flys, 12M : 5 Flys }
        oneMFlys = curatedData[(curatedData['month_of_spread'] == 1) & (
            curatedData['contract_type'] == 'F') & (curatedData['contract_date'] == rdate)].head(19)
        threeMFlys = curatedData[(curatedData['month_of_spread'] == 3) & (
            curatedData['contract_type'] == 'F') & (curatedData['contract_date'] == rdate)].head(8)
        sixMFlys = curatedData[(curatedData['month_of_spread'] == 6) & (
            curatedData['contract_type'] == 'F') & (curatedData['contract_date'] == rdate)].head(10)
        twelveMFlys = curatedData[(curatedData['month_of_spread'] == 12) & (
            curatedData['contract_type'] == 'F') & (curatedData['contract_date'] == rdate)].head(5)

        # Merging all the individual dfs
        activeContracts = pd.concat([oneMFlys, threeMFlys, sixMFlys, twelveMFlys])

        # Calculating Combination Fly Names
        oneMCombinations = pd.DataFrame(combinations(
            oneMFlys['contract_label'], 2), columns=['Fly1', 'Fly2'])
        oneMCombinations = oneMCombinations.assign(
            month_of_spread=1, double_fly=lambda x: x['Fly1'] + '-' + x['Fly2'])

        threeMCombinations = pd.DataFrame(combinations(
            threeMFlys['contract_label'], 2), columns=['Fly1', 'Fly2'])
        threeMCombinations = threeMCombinations.assign(
            month_of_spread=3, double_fly=lambda x: x['Fly1'] + '-' + x['Fly2'])

        sixMCombinations = pd.DataFrame(combinations(
            sixMFlys['contract_label'], 2), columns=['Fly1', 'Fly2'])
        sixMCombinations = sixMCombinations.assign(
            month_of_spread=6, double_fly=lambda x: x['Fly1'] + '-' + x['Fly2'])

        twelveMCombinations = pd.DataFrame(combinations(
            twelveMFlys['contract_label'], 2), columns=['Fly1', 'Fly2'])
        twelveMCombinations = twelveMCombinations.assign(
            month_of_spread=12, double_fly=lambda x: x['Fly1'] + '-' + x['Fly2'])

        # Calculating the Combination Double Fly Values
        allCombinations = pd.concat(
            [oneMCombinations, threeMCombinations, sixMCombinations, twelveMCombinations])

        # print(allCombinations.head().to_string())

        doubleFly_calc = pd.DataFrame(np.round(np.transpose(np.subtract.outer(list(activeContracts['settle']), list(activeContracts['settle']))), 2), 
                                    index = activeContracts['contract_label'] )

        doubleFly_calc.columns = activeContracts['contract_label']

        # print(doubleFly_calc.to_string())

        allCombinations.reset_index(inplace=True)

        def getDoubleFlyValue(fly1, fly2):
            return doubleFly_calc.loc[fly2][fly1]

        allCombinations['df_settle'] = allCombinations.apply(
            lambda x: getDoubleFlyValue(x['Fly1'], x['Fly2']), axis=1)

        # TRADITIONAL RECOMMENDATION LOGIC

        def predictDoubleFly(row):
            outright = row["Fly1"][:3]
            fly1 = row["Fly1"]
            fly2 = row["Fly2"]
            structures = [fly1, fly2, outright]

            df = df_of_interest_wide.loc[:, structures].dropna()
            y = (df[fly1] - df[fly2]).values
            X = df[[outright]].values

            # value = outright_settle[outright]

            tupleX = tuple(X**i for i in range(1, 2 + 1))
            X = np.hstack(tupleX)

            model = LinearRegression()
            model.fit(X, y)
            allValue = model.predict(X)
            predicted = model.predict(X[-1:, ])

            row["Predicted_Df"] = predicted[0]

            standardErrorPoly = np.sqrt(sum((y - allValue)**2) / len(X))
            row['SDAway_DoubleFly'] = (
                y[-1] - row['Predicted_Df']) / standardErrorPoly

            return row

        def predictFly(row):
            structures = [row['contract_label'], row['first_contract_Id']]

            df = df_of_interest_wide.loc[:, structures].dropna()
            y = df[row['contract_label']]
            X = df[[row['first_contract_Id']]].values

            tupleX = tuple(X**i for i in range(1, 2 + 1))
            X = np.hstack(tupleX)

            model = LinearRegression()

            model.fit(X, y)

            allValue = model.predict(X)

            predicted = model.predict(X[-1:, ])

            row["Predicted_Fly"] = predicted[0]

            standardErrorPoly = np.sqrt(sum((y - allValue)**2) / len(X))

            row['SDAway_Fly'] = (
                row['settle'] - row["Predicted_Fly"]) / standardErrorPoly

            return row

        first_outrights = activeContracts["first_contract_Id"].unique()
        third_outrights = activeContracts["contract_label"].str[-3:].unique()
        unique_flys = activeContracts["contract_label"].unique()

        unique_outrights = np.unique(np.concatenate(
            [first_outrights, third_outrights]))

        non_fly_data = curatedData.loc[curatedData["first_contract_Id"].isin(
            unique_outrights) & curatedData["contract_type"].isin(["O"])]

        fly_data = curatedData.loc[curatedData["contract_label"].isin(
            unique_flys)]

        df_of_interest = pd.concat([fly_data, non_fly_data])

        df_of_interest_wide = pd.pivot(
            df_of_interest, index="contract_date", columns="contract_label", values="settle")

        # print(allCombinations.head().to_string())

        # Fly Regression
        activeContracts = activeContracts[[
            'contract_date', 'first_contract_Id', 'contract_label', 'month_of_spread', 'settle']]
        activeContracts = activeContracts.apply(predictFly, axis=1)

        activeContracts = activeContracts[[
            'contract_label', 'settle', 'month_of_spread', 'Predicted_Fly', 'SDAway_Fly']]
        activeContracts.rename(columns={'contract_label': 'Fly', 'settle': 'Actual',
                                        'Predicted_Fly': 'Predicted', 'SDAway_Fly': 'SDs Aways'}, inplace=True)

        # Double Fly Regression
        allCombinations = allCombinations.apply(predictDoubleFly, axis=1)
        allCombinations.rename(columns={'double_fly': 'Structure', 'df_settle': 'Actual',
                                        'Predicted_Df': 'Predicted', 'SDAway_DoubleFly': 'SDs Aways'}, inplace=True)

        def EMAFly(row):
            df = pd.DataFrame(df_of_interest_wide.loc[:, row['Fly']].dropna())

            df = df.assign(EWM50 = df[row['Fly']].ewm(span = 50).mean(), 
                EWM200 = df[row['Fly']].ewm(span = 200).mean())

            df['Trade'] = np.where(df['EWM50'] > df['EWM200'], 'Sell', 'Buy')
            df['Trade+1'] = df.Trade.shift(periods = 1)

            df = df[df['Trade'] != df['Trade+1']]

            df.dropna(inplace = True)

            try: 
                df = df.loc[asOn.strftime("%Y-%m-%d")]
                row['Trade'] = df['Trade']
            except KeyError as e:
                row['Trade'] = 'NA' 

            return row

        def EMADoubleFly(row):
            fly1 = row["Fly1"]
            fly2 = row["Fly2"]
            structures = [fly1, fly2]

            df = pd.DataFrame(df_of_interest_wide.loc[:, structures].dropna())

            df['settle'] = np.round((df[fly1] - df[fly2]).values, 2)
            if fly1 == 'Z20/F21/G21' and fly2 == 'V21/X21/Z21':
                print(df.head(10).to_string())


            df = df[['settle']]

            df = df.assign(EWM50 = df[['settle']].ewm(span = 50).mean(), 
                EWM200 = df[['settle']].ewm(span = 200).mean())

            df['Trade'] = np.where(df['EWM50'] > df['EWM200'], 'Sell', 'Buy')
            df['Trade+1'] = df.Trade.shift(periods = 1)

            df = df[df['Trade'] != df['Trade+1']]

            # df.dropna(inplace = True)

            if fly1 == 'Z20/F21/G21' and fly2 == 'V21/X21/Z21':
                print(df.to_string())

            try: 
                df = df.loc[asOn.strftime("%Y-%m-%d")]
                row['Trade'] = df['Trade']
            except KeyError as e:
                row['Trade'] = 'NA' 

            return row            

        if vno == 3:
            # TECHNIAL INDICATOR BASED RECOMMENDATION LOGIC

            # FLY IMPLEMENTATION
            activeContracts = activeContracts.apply(EMAFly, axis = 1)

            # DOUBLEFLY IMPLEMENTATION
            allCombinations = allCombinations.apply(EMADoubleFly, axis = 1)

            allCombinations = allCombinations[['Structure', 'Actual', 'month_of_spread', 'Predicted', 'SDs Aways', 'Trade']]
        else:
            allCombinations = allCombinations[['Structure', 'Actual', 'month_of_spread', 'Predicted', 'SDs Aways']]

        # FILTERING THE RESULTS

        activeContracts = activeContracts.round(3)

        allCombinations = allCombinations.round(3)

        # print(activeContracts.head())

        # print(allCombinations.to_string())
        
        def filter_value(x):
            if x <= -2.0:
                return "Under"
            elif x >= 2.0:
                return "Over"
            else:
                return np.nan

        tempList = ["Fly", "Actual", "Predicted", "SDsAways", "Value", "FlyMonth"]

        Fly1M = activeContracts[activeContracts['month_of_spread'] == 1].drop(
            'month_of_spread', axis=1)
        Fly1M["Value"]  = Fly1M["SDs Aways"].apply(lambda x: filter_value(x))
        Fly1M.dropna(inplace = True)
        Fly1M["FlyMonth"] = 1
        for i, j in zip(list(Fly1M.columns), tempList):
            Fly1M.rename(columns = {i : j}, inplace = True)
        

        Fly3M = activeContracts[activeContracts['month_of_spread'] == 3].drop(
            'month_of_spread', axis=1)
        Fly3M["Value"]  = Fly3M["SDs Aways"].apply(lambda x: filter_value(x))
        Fly3M.dropna(inplace = True)
        Fly3M["FlyMonth"] = 3
        for i, j in zip(list(Fly3M.columns), tempList):
            Fly3M.rename(columns = {i : j}, inplace = True)


        Fly6M = activeContracts[activeContracts['month_of_spread'] == 6].drop(
            'month_of_spread', axis=1)
        Fly6M["Value"]  = Fly6M["SDs Aways"].apply(lambda x: filter_value(x))
        Fly6M.dropna(inplace = True)
        Fly6M["FlyMonth"] = 6
        for i, j in zip(list(Fly6M.columns), tempList):
            Fly6M.rename(columns = {i : j}, inplace = True)


        Fly12M = activeContracts[activeContracts['month_of_spread'] == 12].drop(
            'month_of_spread', axis=1)
        Fly12M["Value"]  = Fly12M["SDs Aways"].apply(lambda x: filter_value(x))
        Fly12M.dropna(inplace = True)
        Fly12M["FlyMonth"] = 12
        for i, j in zip(list(Fly12M.columns), tempList):
            Fly12M.rename(columns = {i : j}, inplace = True)
        

        tempList = ["Structure", "Actual", "Predicted", "SDsAways", "Value", "Month"]

        DFly1M = allCombinations[allCombinations['month_of_spread'] == 1].drop(
            'month_of_spread', axis=1)
        DFly1M["Value"]  = DFly1M["SDs Aways"].apply(lambda x: filter_value(x))
        DFly1M.dropna(inplace = True)
        DFly1M["Month"] = 1
        for i, j in zip(list(DFly1M.columns), tempList):
            DFly1M.rename(columns = {i : j}, inplace = True)


        DFly3M = allCombinations[allCombinations['month_of_spread'] == 3].drop(
            'month_of_spread', axis=1)
        DFly3M["Value"]  = DFly3M["SDs Aways"].apply(lambda x: filter_value(x))
        DFly3M.dropna(inplace = True)
        DFly3M["Month"] = 3
        for i, j in zip(list(DFly3M.columns), tempList):
            DFly3M.rename(columns = {i : j}, inplace = True)


        DFly6M = allCombinations[allCombinations['month_of_spread'] == 6].drop(
            'month_of_spread', axis=1)
        DFly6M["Value"]  = DFly6M["SDs Aways"].apply(lambda x: filter_value(x))
        DFly6M.dropna(inplace = True)
        DFly6M["Month"] = 6
        for i, j in zip(list(DFly6M.columns), tempList):
            DFly6M.rename(columns = {i : j}, inplace = True)


        DFly12M = allCombinations[allCombinations['month_of_spread'] == 12].drop(
            'month_of_spread', axis=1)
        DFly12M["Value"]  = DFly12M["SDs Aways"].apply(lambda x: filter_value(x))
        DFly12M.dropna(inplace = True)
        DFly12M["Month"] = 12
        for i, j in zip(list(DFly12M.columns), tempList):
            DFly12M.rename(columns = {i : j}, inplace = True)


        flyDF = pd.concat(["Fly1M", "Fly3M", "Fly6M", "Fly12M"])
        flyDF = flyDF.to_dict('records')
        flyDF = json.dumps(flyDF).replace('"', "'")

        dflyDF = pd.concat(["DFly1M", "DFly3M", "DFly6M", "DFly12M"])
        dflyDF = dflyDF.to_dict('records')
        dflyDF = json.dumps(dflyDF).replace('"', "'")

        returnString =  {
                'fly': flyDF,
                'doubleFly': dflyDF
            }

        temp = json.dumps(returnString)
        returnString = temp.replace('"', "'")
        returnString = returnString.replace("'[", "[")
        returnString = returnString.replace("]'", "]")

        return returnString

    except Exception as e:
        return {
            'statusCode': 410,
            'data': '',
            'message': str(logging.error("Exception Occurred", exc_info=True))
        }


if __name__ == "__main__":
    app.run(debug=True, port=5000, host='0.0.0.0')
