# Z-Score or Standard Score

Originally, this metrics comes from Wald-Wolfowitz test for randomness. 
Z-score measures the dependence of the trade outcomes from the previous trade outcomes. 
The values of Z are interpreted in the same way as the probability of deviation from zero for a random value distributed according to the standard normal distribution, i.e mean is 0 and variance is from -3 to +3. 
In case of standard score, 3-sigma rule states that value from -0.05 to +0.05 identifies sequence as normally distributed and random.
Any value outside the interval above identifies the bias, either positive or negative, depending on the sign. 

- Value above +0.05 means that it is most probably that the profitable trade will be followed by a losing one. 
- Value below -0.05 means that the profit will be followed by a profit, a loss will be followed by a loss again. 

$$ Sum = W + L $$

$$ Pro = 2 * W * L $$

$$ Mean = Pro / Sum + 1 $$

$$ Dev = \sqrt{Pro * (Pro - Sum) / (Sum ^ 2 * (Sum - 1))} $$

$$ Score = Runs - Mean / Dev $$

- W - Number of winning trades
- L - Number of losing trades
- Pro - Product of 2 * W * L
- Sum - Sum of W + L
- Runs - Number of runs when trade sign changes 