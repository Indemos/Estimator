# Geometric Holding Period Return 

Along with arithmetic average, Ralph Vince introduces the notion of geometric average that we shall call GHPR.
It is practically always less than the AHPR. 
The geometric average is the growth factor per game. 
The system having the largest GHPR will make the highest profits if we trade on the basis of reinvestment. 
The GHPR below one means that the system will lose money if we trade on the basis of reinvestment.

```
GHPR = (BalanceClose / BalanceOpen) ^ (1 / N)
```

- BalanceClose - Final balance
- BalanceOpen - Initial balance
- N - Number of trades

**CAGR**

Compound average growth rate can be calculated using GHPR. 
The only change is replacing number of trades with number of years. 

- N - Number of years
