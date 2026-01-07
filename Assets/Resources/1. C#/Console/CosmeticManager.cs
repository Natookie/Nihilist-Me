using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CosmeticManager : MonoBehaviour
{
    [System.Serializable]
    public class Cosmetic
    {
        public string id;
        public string displayName;
        public int weight;
        public string rarity;
        public Color color;
    }

    [SerializeField] private List<Cosmetic> allCosmetics = new List<Cosmetic>();
    private List<Cosmetic> inventory = new List<Cosmetic>();
    private HashSet<string> unlockedCosmeticIds = new HashSet<string>();
    
    public const int GACHA_COST = 20;

    void Start(){
        InitializeDefaultCosmetics();
        LoadUnlockedCosmetics();
    }

    void InitializeDefaultCosmetics(){
        if(allCosmetics.Count > 0) return;
        
        allCosmetics = new List<Cosmetic>(){
            new Cosmetic { id = "hat_red", displayName = "Red Hat", weight = 60, rarity = "Common", color = Color.white },
            new Cosmetic { id = "hat_blue", displayName = "Blue Hat", weight = 30, rarity = "Rare", color = Color.cyan },
            new Cosmetic { id = "halo_gold", displayName = "Golden Halo", weight = 10, rarity = "Legendary", color = new Color(1f, 0.84f, 0f) }
        };
    }

    public Cosmetic RollGacha(){
        int totalWeight = allCosmetics.Sum(c => c.weight);
        if(totalWeight <= 0) return allCosmetics[0];

        int roll = Random.Range(0, totalWeight);
        int runningTotal = 0;
        
        foreach(var cosmetic in allCosmetics){
            runningTotal += cosmetic.weight;
            if(roll < runningTotal) return cosmetic;
        }
        
        return allCosmetics.Last();
    }

    public void UnlockCosmetic(Cosmetic cosmetic){
        if(cosmetic == null) return;
        
        if(!unlockedCosmeticIds.Contains(cosmetic.id)){
            unlockedCosmeticIds.Add(cosmetic.id);
            inventory.Add(cosmetic);
            SaveUnlockedCosmetics();
        }
    }

    public bool IsCosmeticUnlocked(string cosmeticId) =>  unlockedCosmeticIds.Contains(cosmeticId);
    public List<Cosmetic> GetInventory() => new List<Cosmetic>(inventory);
    public int GetGachaCost() => GACHA_COST;

    void SaveUnlockedCosmetics(){
        string unlockedIds = string.Join(",", unlockedCosmeticIds);
        PlayerPrefs.SetString("cosmetics_unlocked", unlockedIds);
        PlayerPrefs.Save();
    }

    void LoadUnlockedCosmetics(){
        unlockedCosmeticIds.Clear();
        inventory.Clear();
        
        string savedIds = PlayerPrefs.GetString("cosmetics_unlocked", "");
        if(string.IsNullOrEmpty(savedIds)) return;
        
        string[] ids = savedIds.Split(',');
        foreach(string id in ids){
            if(string.IsNullOrEmpty(id)) continue;
            
            unlockedCosmeticIds.Add(id);
            Cosmetic cosmetic = allCosmetics.Find(c => c.id == id);
            if(cosmetic != null) inventory.Add(cosmetic);
        }
    }
}