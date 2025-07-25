# -*- coding: utf-8 -*-
from backtester import BTStrategy, BTData, BT
import yfinance as yf
import matplotlib.pyplot as plt

class MyStrategy(BTStrategy):
    def __init__(self):
        print("MyStrategy")
        super().__init__()

        self.c=[]
        pass

    def run_at_open(self, data, o):
        pass

    def run_at_close(self, data):
        self.c.append(data[-1].c)

        if len(self.c) >= 20:
            ma5 = sum(self.c[-5:]) / 5
            ma10 = sum(self.c[-20:]) / 20

            if ma5 > ma10:
                self.buy_to_one()
                #logi("buy")
            else:
                self.sell_to_one()
                #logi("sell")


class MyStrategy2(BTStrategy):
    def __init__(self):
        print("MyStrategy")
        super().__init__()

        self.c=[]
        pass

    def run_at_open(self, data, o):
        pass

    def run_at_close(self, data):
        self.buy_to_one()





import dill
def write_dill(fname, data):
    with open(fname, 'wb') as f:
        dill.dump(data, f)

def read_dill(fname):
    """Deserialize data from a dill file."""
    with open(fname, 'rb') as f:
        return dill.load(f)

fname = 'c:\\temp\\a.pickle'
if 1:
    df = yf.Ticker("SPY").history(start="2024-01-01", end="2025-06-23", auto_adjust=True)
    write_dill(fname,df)
else:
    df = read_dill(fname)

o_arr=df[['Open']]
c_arr=df[['Close']]
sz = len(df)


from datetime import datetime

def dt_to_ymd(dt):
    y, m, d = dt.year, dt.month, dt.day
    ymd = y * 10000 + m * 100 + d
    return ymd

def dt_to_hms(dt):
    h, m, s = dt.hour, dt.minute, dt.second
    hms = h * 10000 + m * 100 + s
    return hms

def ymd_to_dt(ymd):
    y = ymd // 10000
    m = (ymd % 10000) // 100
    d = ymd % 100
    return datetime(y, m, d)

def hms_to_dt(hms):
    h = hms // 10000
    m = (hms % 10000) // 100
    s = hms % 100
    return h, m, s  # typically returned as a tuple

def ymd_hms_to_dt(ymd, hms):
    y = ymd // 10000
    m = (ymd % 10000) // 100
    d = ymd % 100
    h = hms // 10000
    mi = (hms % 10000) // 100
    s = hms % 100
    return datetime(y, m, d, h, mi, s)

data = []

for idx, row in df.iterrows():
    d = BTData()
    dt = idx.tz_localize(None)
    ymd = dt_to_ymd(dt)
    hms = dt_to_hms(dt)
    d.ymd = ymd
    d.hms = hms
    d.o = row['Open']
    d.h = row['High']
    d.l = row['Low']
    d.c = row['Close']
    d.v = row['Volume']
    data.append(d)





strategy = MyStrategy()
bt = BT()
bt.run(strategy, data)  # this will properly run your backtest using your dataset

if 1:
    plt.plot(strategy.pt_win_realtime)
    plt.title("Realtime P&L")
    plt.xlabel("Time (ticks)")
    plt.ylabel("Profit / Loss")
    plt.grid(True)
    plt.show()

'''
strategy = MyStrategy2()
bt = BT()
bt.run(strategy, data)  # this will properly run your backtest using your dataset

plt.plot(strategy.pt_win_realtime)
plt.title("Realtime P&L")
plt.xlabel("Time (ticks)")
plt.ylabel("Profit / Loss")
plt.grid(True)
plt.show()
'''