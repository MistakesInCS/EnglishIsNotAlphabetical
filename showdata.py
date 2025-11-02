import seaborn as sns
import json
from pandas import DataFrame
import matplotlib.pyplot as plt
from statistics import stdev, mean

GDFile = r'.\GD.json'
GDFile2 = r'.\GD2.json'
GDFile3 = r'.\GD3.json'
GAFile = r'.\GA.json'
RandFile = r'.\Random.json'

def LoadScores(file):
	with open(file, 'r') as f:
            data = json.load(f)
            scores = [item.get("Score", 0) for item in data]
            return scores
    
GDData = LoadScores(GDFile)
GDData2 = LoadScores(GDFile2)
GDData3 = LoadScores(GDFile3)
GAData = LoadScores(GAFile)
RandData = LoadScores(RandFile)

CombinedData = []
CombinedData.extend([{"Score": score, "Source": "Random"} for score in RandData])
CombinedData.extend([{"Score": score, "Source": "GD, no momentum"} for score in GDData])
CombinedData.extend([{"Score": score, "Source": "GD, mom = 0.9"} for score in GDData2])
CombinedData.extend([{"Score": score, "Source": "GD, mom = 0.3"} for score in GDData3])
CombinedData.extend([{"Score": score, "Source": "GA"} for score in GAData])

sns.set_theme()

sns.displot(DataFrame(data = CombinedData), x="Score", hue="Source", kde=True, rug=False, common_norm=False, fill=True, bins=200, stat="density")
#sns.violinplot(DataFrame(data = CombinedData), x="Score", y="Source", hue="Source", dodge=False, common_norm=False)
plt.show()

for a in [GDData, GDData2, GDData3, GAData, RandData]:
      print(f"max: {max(a)}, min: {min(a)}, avg: {mean(a)}, std: {stdev(a)}")
