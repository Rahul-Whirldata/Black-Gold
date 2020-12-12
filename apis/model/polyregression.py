import statsmodels.formula.api as sm
import logging
import numpy as np
import json

def getPolyRegressionLine(data):

    try:
        poly = 2
        df = data
        columns = list(df.columns)
        dateidx = 0
        if 'date' in columns:
            dateidx = columns.index('date')
        if 'Date' in columns:
            dateidx = columns.index('Date')

        columns = columns[:dateidx] + \
            columns[dateidx + 1:] + [columns[dateidx]]
        df = df[columns]

        colDict = dict()
        colList = []
        i = 1
        for col in columns:
            if (col != 'date') and (col != 'Date'):
                if len(colList) == 0:
                    colList.append('Y')
                    colDict['Y'] = col
                else:
                    colList.append('X' + str(i))
                    colDict['X' + str(i)] = col
                    i = i + 1
            else:
                colList.append(col)
                colDict[col] = col
        if len(columns) == 3:
            colList[colList.index('X1')] = 'X'
            colDict['X'] = colDict['X1']
        df.columns = colList

        formula = 'Y ~ '
        for col in colList[1:]:
            if (col != 'date') and (col != 'Date') and (col != 'Y'):
                formula = formula + col + ' + '
        formula = formula[:-2]

        formulaPoly = 'Y ~ '
        polyEquationLst = []
        for col in colList[1:]:
            if (col != 'date') and (col != 'Date') and (col != 'Y'):
                polyExpression = ' + '.join(['I(X ** {})'.format(float(i))
                                             for i in range(2, poly + 1)])
                if poly > 1:
                    formulaPoly = formulaPoly + col + ' + ' + polyExpression
                    polyEquationLst.append(col)
                    polyEquationLst.extend(
                        ['I(X ** {})'.format(float(i)) for i in range(2, poly + 1)])
                else:
                    formulaPoly = formulaPoly + col

        resultPoly = sm.ols(formula=formulaPoly, data=df).fit()
        df.dropna(axis=0, inplace=True)
        df['polyRegression'] = resultPoly.predict(df)
        rsquaredPoly = round(resultPoly.rsquared, 3)
        df['standardErrorPoly'] = df['Y'] - df['polyRegression']
        # df['standardErrorPoly'] =  df['polyRegression'] - df['Y']
        standardErrorPoly = np.sqrt(
            sum((df['Y'] - df['polyRegression'])**2) / len(df))

        result = sm.ols(formula=formula, data=df).fit()
        df['Regression'] = result.predict(df)

        df['standardError'] = df['Y'] - df['Regression']
        # df['standardError'] =  df['Regression'] - df['Y']
        standardError = np.sqrt(sum((df['Y'] - df['Regression'])**2) / len(df))

        df.sort_values(by='X', inplace=True)

        columns = list(df.columns)
        for col in colList:
            columns[columns.index(col)] = colDict[col]
        df.columns = columns

        params = result.params
        eqution = 'Y = ' + str(round(params['Intercept'], 2))
        for col in colList:
            if (col != 'date') and (col != 'Date') and (col != 'Y'):
                eqution = eqution + ' {} '.format('-' if params[col] < 0 else '+') + str(
                    abs(round(params[col], 2))) + '*' + col
        rsquared = round(result.rsquared, 3)

        # For PolyEquation Text
        paramsPoly = resultPoly.params
        polyEquation = 'Y = ' + str(round(paramsPoly['Intercept'], 2))
        for col in polyEquationLst:
            if (col != 'date') and (col != 'Date') and (col != 'Y'):
                polyEquation = polyEquation + ' {} '.format('-' if paramsPoly[col] < 0 else '+') + str(
                    abs(round(paramsPoly[col], 2))) + '*'
                if 'I' in col:
                    col = col[2:-3]
                polyEquation = polyEquation + col
        polyEquation = polyEquation.replace(" ** ", "^")

        # df.sort_values(by='date', inplace=True)

        df['date'] = df['date'].apply(lambda x: str(x))
        
        tempList = ["Scattery", "Chartx", "Date", "PolyRegressiony", "StandardErrorPoly", "Regressiony", "Standard"]
        
        for i, j in zip(list(df.columns), tempList):
            df.rename(columns = {i : j}, inplace = True)

        # df["Scattery"] = df["Scattery"].apply(lambda x: round(x, 2))
        # df["Chartx"] = df["Chartx"].apply(lambda x: round(x, 1))
        # df["PolyRegressiony"] = df["PolyRegressiony"].apply(lambda x: round(x, 2))
        # df["Regressiony"] = df["Regressiony"].apply(lambda x: round(x, 2))
        # df["StandardErrorPoly"] = df["StandardErrorPoly"].apply(lambda x: round(x, 3))
        # df["Standard"] = df["Standard"].apply(lambda x: round(x, 3))

        data = df.to_dict('records')
        data = json.dumps(data).replace('"', "'")

        return data, rsquared, rsquaredPoly, eqution, polyEquation, standardError, standardErrorPoly

    except Exception as e:
        return str(logging.error("Exception Occurred", exc_info=True))
