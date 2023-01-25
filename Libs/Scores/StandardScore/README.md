# Z-Score or Standard Score

Z-score measures the dependence of the trade outcomes from the previous trade outcomes. 
The values of Z are interpreted in the same way as the probability of deviation from zero for a random value distributed according to the standard normal distribution, i.e mean = 0, sigma = 1. 
If the probability of a normally distributed random value within the range of ±3σ is 99.74%, expanding outside of this interval with the same probability of 99.74% informs us that this random value does not belong to this given normal distribution. 
This is why the "3-sigma rule" is read as: a normal random value deviates from its average by no more than 3-sigma distance.
Sign of Z informs us about the type of dependence. 
Plus means that it is most probably that the profitable trade will be followed by a losing one. 
Minus says that the profit will be followed by a profit, a loss will be followed by a loss again

```
Z = (N * (R - 0.5) - P) / ((P * (P - N)) / (N - 1)) ^ (1 / 2)
```

- N - Total number of trades
- R - Total number of losing and winning series
- W - Number of winning trades
- L - Number of losing trades
- P - 2 * W * L
