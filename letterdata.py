import json
import copy

GDFile = r'.\GD.json'
GDFile2 = r'.\GD2.json'
GDFile3 = r'.\GD3.json'
GAFile = r'.\GA.json'
RandFile = r'.\Random.json'

def LoadScores(file):
	with open(file, 'r') as f:
            data = json.load(f)
            #scores = [item.get("Score", 0) for item in data]
            return data
    
GDData = LoadScores(GDFile)
GDData2 = LoadScores(GDFile2)
GDData3 = LoadScores(GDFile3)
GAData = LoadScores(GAFile)
RandData = LoadScores(RandFile)

def GetLetterPos(order):
      alphList = [0] * 26
      for index in range(26):
            alphList[ord(order[index]) - ord('a')] = index # Find the index-th letter, then set that point in the alph list to the index
      return alphList

def AverageLetterPos(data):
      totalAlphList = [0] * 26
      for datum in data:
            totalAlphList = [x + y for x, y in zip(totalAlphList, GetLetterPos(datum["Order"]))]
      for index in range(26):
            totalAlphList[index] = totalAlphList[index]/len(data)
      return totalAlphList

def WeightedLetterPos(data):
      min_element = min(data, key = lambda x: x["Score"])
      worst_order = min_element["Order"]
      min_element = min_element["Score"]
      best_order = max(data, key = lambda x: x["Score"])["Order"]
      print(f"Worst order: {worst_order}, best order: {best_order}")
      totalAlphList = [0] * 26
      totalScore = 0
      for datum in data:
            totalAlphList = [x + y*(datum["Score"]-min_element) for x, y in zip(totalAlphList, GetLetterPos(datum["Order"]))]
            totalScore += (datum["Score"] - min_element)
      for index in range(26):
            totalAlphList[index] = totalAlphList[index]/len(data)/totalScore*100
      return totalAlphList

def TotalWeightedPos(data):
      totalAlphList = [0] * 26
      totalScore = 0
      for datum in data:
            totalAlphList = [x + y*(datum["Score"]) for x, y in zip(totalAlphList, GetLetterPos(datum["Order"]))]
            totalScore += (datum["Score"])
      for index in range(26):
            totalAlphList[index] = totalAlphList[index]/len(data)/totalScore*100
      return totalAlphList

def GreedilyAssignLetters(position_list):
      cpy_list = list(zip(copy.deepcopy(position_list), range(26)))
      cpy_list.sort(key = lambda pos: pos[0])
      alph_idxs = [item[1] for item in cpy_list]
      new_alph = ""
      for index in alph_idxs:
            new_alph += chr(ord('a') + index)
      return new_alph


def print_all_my_stuff(data):
      avg_list = AverageLetterPos(data)
      wht_list = WeightedLetterPos(data)
      for x in avg_list:
            print(f" {x} |", end = "")
      print('\n')

      for x in wht_list:
            print(f" {x} |", end = "")
      print('\n')

      print(GreedilyAssignLetters(avg_list))
      print(GreedilyAssignLetters(wht_list))
      return avg_list, wht_list

def OutputAveragePos(kwargs):
      print("| Letter | Random Average | Random Weighted | Grad. Desc. Average | Grad. Desc. Weighted | Grad. Desc. Mom. = 0.9 Average | Grad. Desc. Mom. = 0.9 Weighted | Grad. Desc. Mom. = 0.3 Average | Grad. Desc. Mom. = 0.3 Weighted | Genetic Alg. Average | Grenetic Alg. Weighted |")
      for i in range(26):
            print(f"| {chr(i + ord('a'))} | {kwargs["RandData"][0][i]:.2f} | {kwargs["RandData"][1][i]:.2f} | {kwargs["GDData"][0][i]:.2f} | {kwargs["GDData"][1][i]:.2f} | {kwargs["GDData2"][0][i]:.2f} | {kwargs["GDData2"][1][i]:.2f} | {kwargs["GDData3"][0][i]:.2f} | {kwargs["GDData3"][1][i]:.2f} | {kwargs["GAData"][0][i]:.2f} | {kwargs["GAData"][1][i]:.2f} |")

all_data = {}
print(f"RAND: ")
all_data["RandData"] = print_all_my_stuff(RandData)
print(f"GDDATA: ")
all_data["GDData"] = print_all_my_stuff(GDData)
print(f"GDDATA2: ")
all_data["GDData2"] = print_all_my_stuff(GDData2)
print(f"GDDATA3: ")
all_data["GDData3"] = print_all_my_stuff(GDData3)
print(f"GADATA: ")
all_data["GAData"] = print_all_my_stuff(GAData)
OutputAveragePos(all_data)

