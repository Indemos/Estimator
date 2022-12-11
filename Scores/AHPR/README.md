# Average Holding Period Return

In his book, The Mathematics of Money Management, Ralph Vince uses the notion of HPR. 
A trade resulted in profit of 10% 

```
HPR = 1 + 0.10 = 1.10
```

A trade resulted in a loss of 10%. 

```
HPR = 1 - 0. 10 = 0.90
```

You can also obtain the value of HPR for a trade by dividing the balance value after the trade has been closed by the balance value at opening of the trade. 

```
HPR = BalanceClose / BalanceOpen
```

Thus, every trade has both a result in money terms and a result expressed as HPR. 
This will allow us to compare systems independently on the size of traded contracts. 
One of indexes used in such comparison is the arithmetic average, AHPR. 
AHPR can be found as the arithmetic average of all HPRs.

```
AHPR = Sum(1 + HPR[i]) / N
```

- N - Number of trades
