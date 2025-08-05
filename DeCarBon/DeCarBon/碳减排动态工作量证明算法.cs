using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.IO;
using Newtonsoft.Json;
using System.Linq;
using System.Threading.Tasks;
using static CarbonAwareBlockchain.CarbonReductionCalculator;

namespace CarbonAwareBlockchain
{
    /// <summary>
    /// 逆变器实时数据（来自光伏系统）
    /// </summary>
    public class InverterData
    {
        public string InverterId { get; set; }       // 逆变器唯一ID
        public string PanelId { get; set; }          // 关联的钙钛矿组件ID
        public DateTime Timestamp { get; set; }      // 数据时间戳
        public double PowerOutput { get; set; }      // 实时功率输出 (kW)
        public double Irradiance { get; set; }       // 辐照度 (W/m²)
        public double Temperature { get; set; }      // 组件温度 (°C)
        public double EnergyGenerated { get; set; }  // 当日累计发电量 (kWh)
        public string Location { get; set; }         // 地理位置 (经纬度或区域编码)
        public string Signature { get; set; }        // 数据签名 (防篡改)

        public override string ToString()
        {
            return $"[{Timestamp:HH:mm}] {PowerOutput}kW | {Irradiance}W/m² | {Temperature}°C";
        }
    }

    /// <summary>
    /// 区域电网碳排放因子
    /// </summary>
    public class GridEmissionFactor
    {
        public string RegionCode { get; set; }       // 区域代码 (CN-HB, CN-ZJ, 等)
        public string RegionName { get; set; }       // 区域名称
        public double EmissionFactor { get; set; }   // 碳排放因子 (kgCO₂/kWh)
        public DateTime LastUpdated { get; set; }    // 最后更新时间

        // 中国主要区域排放因子 (来源: 中国区域电网基准线排放因子)
        public static readonly Dictionary<string, double> DefaultFactors = new Dictionary<string, double>
        {
            {"CN-HB", 0.920},  // 华北
            {"CN-NE", 0.776},  // 东北
            {"CN-EC", 0.681},  // 华东
            {"CN-SC", 0.587},  // 华南
            {"CN-NW", 0.724},  // 西北
            {"CN-SW", 0.628}   // 西南
        };
    }

    /// <summary>
    /// 钙钛矿组件碳足迹数据
    /// </summary>
    public class PerovskiteCarbonFootprint
    {
        public string PanelId { get; set; }              // 组件ID
        public double ManufacturingFootprint { get; set; } // 制造碳足迹 (kgCO₂)
        public double Efficiency { get; set; }            // 标称效率 (%)
        public double Size { get; set; }                 // 组件尺寸 (m²)
        public int Lifetime { get; set; }                 // 预期寿命 (年)
        
        /// <summary>
        /// 计算单位发电量碳足迹 (kgCO₂/kWh)
        /// </summary>
        public double CalculateCarbonIntensity(double solarIrradiance = 1500)
        {
            double annualEnergy = (Efficiency / 100) * Size * solarIrradiance;
            double lifetimeEnergy = annualEnergy * Lifetime;
            return ManufacturingFootprint / lifetimeEnergy;
        }
    }

    /// <summary>
    /// 碳减排计算引擎
    /// </summary>
    public static class CarbonReductionCalculator
    {
        // 区域光照分类
        public enum SolarRegionType
        {
            TypeI,  // 青藏高原 (年等效日照 > 1750 kWh/m²)
            TypeII, // 北方大部 (1400-1750 kWh/m²)
            TypeIII // 南方大部 (<1400 kWh/m²)
        }

        /// <summary>
        /// 计算实时碳减排量
        /// </summary>
        public static double CalculateRealTimeReduction(
            InverterData inverterData, 
            PerovskiteCarbonFootprint panelData,
            GridEmissionFactor gridFactor)
        {
            // 1. 计算理论发电量 (考虑温度和辐照度)
            double tempCoefficient = -0.0045; // 温度系数 (%/°C)
            double tempAdjustment = 1 + (tempCoefficient * (inverterData.Temperature - 25));
            double theoreticalOutput = (panelData.Efficiency / 100) * panelData.Size * 
                                      (inverterData.Irradiance / 1000) * tempAdjustment;
            
            // 2. 计算实际减排量
            double carbonIntensity = panelData.CalculateCarbonIntensity();
            double gridEmission = inverterData.EnergyGenerated * gridFactor.EmissionFactor;
            double panelEmission = inverterData.EnergyGenerated * carbonIntensity;
            
            return gridEmission - panelEmission;
        }

        /// <summary>
        /// 获取区域光照类型
        /// </summary>
        public static SolarRegionType GetSolarRegionType(string regionCode)
        {
            // 简化实现 - 实际应根据地理数据计算
            return regionCode switch
            {
                "CN-XZ" => SolarRegionType.TypeI,   // 西藏
                "CN-QH" => SolarRegionType.TypeI,   // 青海
                "CN-XJ" => SolarRegionType.TypeII,  // 新疆
                "CN-GS" => SolarRegionType.TypeII,  // 甘肃
                "CN-NM" => SolarRegionType.TypeII,  // 内蒙古
                _ => SolarRegionType.TypeIII        // 其他地区
            };
        }

        /// <summary>
        /// 获取区域补偿系数
        /// </summary>
        public static double GetRegionCompensationFactor(SolarRegionType regionType)
        {
            return regionType switch
            {
                SolarRegionType.TypeI => 0.9,
                SolarRegionType.TypeII => 1.0,
                SolarRegionType.TypeIII => 1.2,
                _ => 1.0
            };
        }

        /// <summary>
        /// 计算有效碳减排量 (考虑地域补偿)
        /// </summary>
        public static double CalculateEffectiveReduction(
            double rawReduction, 
            SolarRegionType regionType)
        {
            return rawReduction * GetRegionCompensationFactor(regionType);
        }
    }

    /// <summary>
    /// 区块链区块结构 (扩展碳减排数据)
    /// </summary>
    public class CarbonBlock
    {
        public int Index { get; set; }                 // 区块高度
        public DateTime Timestamp { get; set; }         // 时间戳
        public string PreviousHash { get; set; }        // 前一区块哈希
        public string Hash { get; set; }               // 当前区块哈希
        public List<InverterData> InverterData { get; set; } // 逆变器数据
        public double TotalCarbonReduction { get; set; } // 区块总碳减排量
        public int Difficulty { get; set; }             // 挖矿难度
        public int Nonce { get; set; }                  // 工作量证明随机数
        public string Miner { get; set; }               // 矿工地址

        public CarbonBlock()
        {
            InverterData = new List<InverterData>();
            Timestamp = DateTime.UtcNow;
        }

        public string CalculateHash()
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                string rawData = $"{Index}{Timestamp:O}{PreviousHash}{Nonce}{TotalCarbonReduction}";
                foreach (var data in InverterData)
                {
                    rawData += $"{data.InverterId}{data.EnergyGenerated}";
                }
                
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(rawData));
                return BitConverter.ToString(bytes).Replace("-", "");
            }
        }
    }

    /// <summary>
    /// 动态工作量证明算法
    /// </summary>
    public static class CarbonProofOfWork
    {
        // 基础目标难度 (前导零个数)
        private const int BaseDifficulty = 4;
        
        // 难度调整参数
        private const double DifficultySensitivity = 0.8;
        private const double MaxDifficulty = 8;
        private const double MinDifficulty = 2;

        /// <summary>
        /// 执行工作量证明
        /// </summary>
        public static (string hash, int nonce) MineBlock(CarbonBlock block)
        {
            int nonce = 0;
            string hash;
            
            do
            {
                nonce++;
                block.Nonce = nonce;
                hash = block.CalculateHash();
            } while (!IsHashValid(hash, block.Difficulty));
            
            return (hash, nonce);
        }

        /// <summary>
        /// 验证哈希是否满足难度要求
        /// </summary>
        public static bool IsHashValid(string hash, int difficulty)
        {
            return hash.StartsWith(new string('0', difficulty));
        }

        /// <summary>
        /// 计算动态挖矿难度
        /// </summary>
        public static int CalculateDynamicDifficulty(
            double carbonReduction, 
            double referenceReduction,
            SolarRegionType regionType)
        {
            // 1. 计算减排量影响因子
            double reductionFactor = Math.Log(carbonReduction / referenceReduction + 1);
            
            // 2. 应用区域补偿系数
            double regionFactor = CarbonReductionCalculator.GetRegionCompensationFactor(regionType);
            
            // 3. 计算基础难度调整
            double difficultyAdjustment = BaseDifficulty * (1 - DifficultySensitivity * reductionFactor * regionFactor);
            
            // 4. 应用边界限制
            int finalDifficulty = (int)Math.Round(Math.Max(MinDifficulty, Math.Min(MaxDifficulty, difficultyAdjustment)));
            
            return finalDifficulty;
        }
    }

    /// <summary>
    /// 区块链系统核心
    /// </summary>
    public class CarbonAwareBlockchain
    {
        public List<CarbonBlock> Chain { get; private set; }
        public List<InverterData> PendingData { get; private set; }
        public Dictionary<string, PerovskiteCarbonFootprint> PanelRegistry { get; private set; }
        public Dictionary<string, GridEmissionFactor> GridFactors { get; private set; }
        public double ReferenceCarbonReduction { get; set; } = 1000; // 基准减排量 (kgCO₂)
        
        public CarbonAwareBlockchain()
        {
            Chain = new List<CarbonBlock>();
            PendingData = new List<InverterData>();
            PanelRegistry = new Dictionary<string, PerovskiteCarbonFootprint>();
            GridFactors = LoadDefaultGridFactors();
            
            CreateGenesisBlock();
        }
        
        // 加载默认电网排放因子
        private Dictionary<string, GridEmissionFactor> LoadDefaultGridFactors()
        {
            var factors = new Dictionary<string, GridEmissionFactor>();
            foreach (var kvp in GridEmissionFactor.DefaultFactors)
            {
                factors.Add(kvp.Key, new GridEmissionFactor
                {
                    RegionCode = kvp.Key,
                    RegionName = GetRegionName(kvp.Key),
                    EmissionFactor = kvp.Value,
                    LastUpdated = DateTime.UtcNow
                });
            }
            return factors;
        }
        
        private string GetRegionName(string regionCode)
        {
            return regionCode switch
            {
                "CN-HB" => "华北电网",
                "CN-NE" => "东北电网",
                "CN-EC" => "华东电网",
                "CN-SC" => "华南电网",
                "CN-NW" => "西北电网",
                "CN-SW" => "西南电网",
                _ => "未知区域"
            };
        }

        private void CreateGenesisBlock()
        {
            var genesis = new CarbonBlock
            {
                Index = 0,
                Timestamp = DateTime.Parse("2023-01-01T00:00:00Z"),
                PreviousHash = "0",
                Difficulty = 4,
                TotalCarbonReduction = 0,
                Miner = "system"
            };
            
            genesis.Hash = genesis.CalculateHash();
            Chain.Add(genesis);
        }
        
        /// <summary>
        /// 注册钙钛矿组件
        /// </summary>
        public void RegisterPerovskitePanel(PerovskiteCarbonFootprint data)
        {
            if (PanelRegistry.ContainsKey(data.PanelId))
                throw new InvalidOperationException("组件已注册");
            
            PanelRegistry[data.PanelId] = data;
        }
        
        /// <summary>
        /// 添加逆变器数据
        /// </summary>
        public bool AddInverterData(InverterData data)
        {
            // 验证数据签名
            if (!VerifyDataSignature(data))
                return false;
                
            PendingData.Add(data);
            return true;
        }
        
        private bool VerifyDataSignature(InverterData data)
        {
            // 简化实现 - 实际应使用ECDsa验证
            return !string.IsNullOrEmpty(data.Signature) && 
                   data.Signature.Length == 64;
        }
        
        /// <summary>
        /// 计算区块总碳减排量
        /// </summary>
        public double CalculateBlockCarbonReduction()
        {
            double totalReduction = 0;
            
            foreach (var data in PendingData)
            {
                if (!PanelRegistry.TryGetValue(data.PanelId, out var panelData))
                    continue;
                
                var regionCode = ParseRegionFromLocation(data.Location);
                if (!GridFactors.TryGetValue(regionCode, out var gridFactor))
                    continue;
                
                var rawReduction = CarbonReductionCalculator.CalculateRealTimeReduction(
                    data, panelData, gridFactor);
                
                var regionType = CarbonReductionCalculator.GetSolarRegionType(regionCode);
                var effectiveReduction = CarbonReductionCalculator.CalculateEffectiveReduction(
                    rawReduction, regionType);
                
                totalReduction += effectiveReduction;
            }
            
            return totalReduction;
        }
        
        public string ParseRegionFromLocation(string location)
        {
            // 简化实现 - 实际应使用GIS服务
            if (location.Contains("北京") || location.Contains("天津") || 
                location.Contains("河北") || location.Contains("山西") || 
                location.Contains("山东") || location.Contains("内蒙古"))
                return "CN-HB";
            
            if (location.Contains("上海") || location.Contains("江苏") || 
                location.Contains("浙江") || location.Contains("安徽"))
                return "CN-EC";
            
            if (location.Contains("广东") || location.Contains("广西") || 
                location.Contains("福建") || location.Contains("海南"))
                return "CN-SC";
            
            if (location.Contains("西藏") || location.Contains("青海"))
                return "CN-XZ"; // 特殊处理青藏地区
            
            return "CN-EC"; // 默认华东
        }
        
        /// <summary>
        /// 挖矿创建新区块
        /// </summary>
        public void MineBlock(string minerAddress)
        {
            if (PendingData.Count == 0)
                throw new InvalidOperationException("没有待处理数据");
            
            // 1. 计算总碳减排量
            double totalCarbonReduction = CalculateBlockCarbonReduction();
            
            // 2. 确定区域类型 (以主要数据源为准)
            var primaryRegion = GetPrimaryRegion();
            var regionType = CarbonReductionCalculator.GetSolarRegionType(primaryRegion);
            
            // 3. 计算动态难度
            int difficulty = CarbonProofOfWork.CalculateDynamicDifficulty(
                totalCarbonReduction, ReferenceCarbonReduction, regionType);
            
            var lastBlock = Chain.Last();
            var newBlock = new CarbonBlock
            {
                Index = lastBlock.Index + 1,
                PreviousHash = lastBlock.Hash,
                InverterData = new List<InverterData>(PendingData),
                TotalCarbonReduction = totalCarbonReduction,
                Difficulty = difficulty,
                Miner = minerAddress
            };
            
            // 4. 执行工作量证明
            (newBlock.Hash, newBlock.Nonce) = CarbonProofOfWork.MineBlock(newBlock);
            
            // 5. 添加到区块链
            Chain.Add(newBlock);
            
            // 6. 更新基准减排量 (每10个区块调整一次)
            if (newBlock.Index % 10 == 0)
            {
                UpdateReferenceReduction();
            }
            
            // 7. 清空待处理数据
            PendingData.Clear();
            
            Console.WriteLine($"新区块 #{newBlock.Index} 已生成 | 减排量: {totalCarbonReduction:F2} kgCO₂ | 难度: {difficulty}");
        }
        
        private string GetPrimaryRegion()
        {
            // 统计主要区域
            var regionCount = new Dictionary<string, int>();
            foreach (var data in PendingData)
            {
                string region = ParseRegionFromLocation(data.Location);
                if (regionCount.ContainsKey(region))
                    regionCount[region]++;
                else
                    regionCount[region] = 1;
            }
            
            return regionCount.OrderByDescending(kv => kv.Value).First().Key;
        }
        
        private void UpdateReferenceReduction()
        {
            // 计算最近10个区块的平均减排量
            double sum = Chain.Skip(Math.Max(0, Chain.Count - 10)).Sum(b => b.TotalCarbonReduction);
            double average = sum / Math.Min(10, Chain.Count - 1);
            
            // 平滑更新参考值
            ReferenceCarbonReduction = 0.7 * ReferenceCarbonReduction + 0.3 * average;
            Console.WriteLine($"更新基准减排量: {ReferenceCarbonReduction:F2} kgCO₂");
        }
        
        /// <summary>
        /// 验证区块链完整性
        /// </summary>
        public bool ValidateChain()
        {
            for (int i = 1; i < Chain.Count; i++)
            {
                CarbonBlock current = Chain[i];
                CarbonBlock previous = Chain[i - 1];
                
                // 验证哈希链接
                if (current.PreviousHash != previous.Hash)
                    return false;
                
                // 验证区块哈希
                if (current.Hash != current.CalculateHash())
                    return false;
                
                // 验证工作量证明
                if (!CarbonProofOfWork.IsHashValid(current.Hash, current.Difficulty))
                    return false;
            }
            return true;
        }
    }
    
    /// <summary>
    /// 逆变器数据模拟器
    /// </summary>
    public static class InverterSimulator
    {
        private static readonly Random random = new Random();
        private static readonly string[] locations = { "北京", "上海", "广州", "西藏拉萨", "浙江杭州", "江苏南京" };
        
        public static InverterData GenerateSimulatedData(string panelId)
        {
            string location = locations[random.Next(locations.Length)];
            double irradiance = GetRegionalIrradiance(location);
            
            return new InverterData
            {
                InverterId = $"INV-{Guid.NewGuid().ToString().Substring(0, 8)}",
                PanelId = panelId,
                Timestamp = DateTime.UtcNow,
                PowerOutput = Math.Round(irradiance * (0.8 + random.NextDouble() * 0.4) / 1000, 2), // kW
                Irradiance = irradiance,
                Temperature = 25 + (random.NextDouble() - 0.5) * 15, // 15-35°C范围
                EnergyGenerated = Math.Round(random.NextDouble() * 50, 2), // 0-50 kWh
                Location = location,
                Signature = GenerateSignature()
            };
        }
        
        private static double GetRegionalIrradiance(string location)
        {
            // 模拟不同地区辐照度差异
            return location switch
            {
                "西藏拉萨" => 800 + random.NextDouble() * 200, // 高原地区辐照度高
                "北京" => 600 + random.NextDouble() * 150,
                "上海" => 500 + random.NextDouble() * 100,
                "广州" => 550 + random.NextDouble() * 120,
                _ => 500 + random.NextDouble() * 150
            };
        }
        
        private static string GenerateSignature()
        {
            // 生成模拟签名
            byte[] buffer = new byte[32];
            random.NextBytes(buffer);
            return BitConverter.ToString(buffer).Replace("-", "").Substring(0, 64);
        }
    }

    /// <summary>
    /// 系统监控与报告
    /// </summary>
    public class BlockchainMonitor
    {
        private readonly CarbonAwareBlockchain blockchain;
        
        public BlockchainMonitor(CarbonAwareBlockchain blockchain)
        {
            this.blockchain = blockchain;
        }
        
        public void GenerateCarbonReport()
        {
            Console.WriteLine("\n=== 碳减排区块链报告 ===");
            Console.WriteLine($"当前区块高度: {blockchain.Chain.Count - 1}");
            Console.WriteLine($"基准减排量: {blockchain.ReferenceCarbonReduction:F2} kgCO₂");
            
            double totalReduction = blockchain.Chain.Sum(b => b.TotalCarbonReduction);
            Console.WriteLine($"总碳减排量: {totalReduction:F2} kgCO₂");
            
            // 按区域统计
            var regionStats = new Dictionary<string, (double reduction, int count)>();
            foreach (var block in blockchain.Chain.Skip(1))
            {
                foreach (var data in block.InverterData)
                {
                    string region = blockchain.ParseRegionFromLocation(data.Location);
                    if (!regionStats.ContainsKey(region))
                        regionStats[region] = (0, 0);
                    
                    regionStats[region] = (
                        regionStats[region].reduction + data.EnergyGenerated, 
                        regionStats[region].count + 1);
                }
            }
            
            Console.WriteLine("\n按区域统计:");
            foreach (var kvp in regionStats.OrderByDescending(kv => kv.Value.reduction))
            {
                string regionName = blockchain.GridFactors.TryGetValue(kvp.Key, out var factor) 
                    ? factor.RegionName : "未知区域";
                    
                Console.WriteLine($"{regionName}: {kvp.Value.reduction:F1} kWh | {kvp.Value.count} 条数据");
            }
        }
        
        public void AnalyzeDifficultyTrend()
        {
            Console.WriteLine("\n=== 挖矿难度趋势分析 ===");
            var lastBlocks = blockchain.Chain.Skip(Math.Max(0, blockchain.Chain.Count - 20));
            
            Console.WriteLine("区块高度 | 减排量(kgCO₂) | 难度 | 矿工");
            foreach (var block in lastBlocks)
            {
                Console.WriteLine($"{block.Index} | {block.TotalCarbonReduction,8:F2} | {block.Difficulty} | {block.Miner}");
            }
        }
    }

    class Program
    {
        static async Task Main(string[] args)
        {
            var blockchain = new CarbonAwareBlockchain();
            var monitor = new BlockchainMonitor(blockchain);
            
            // 注册示例钙钛矿组件
            var panelData = new PerovskiteCarbonFootprint
            {
                PanelId = "PVK-2023-001",
                ManufacturingFootprint = 1200, // 制造碳足迹 (kgCO₂)
                Efficiency = 22.5,            // 转换效率 (%)
                Size = 1.8,                   // 尺寸 (m²)
                Lifetime = 25                  // 寿命 (年)
            };
            blockchain.RegisterPerovskitePanel(panelData);
            
            Console.WriteLine("钙钛矿碳减排区块链系统启动...");
            Console.WriteLine("模拟数据生成中...");
            
            // 模拟矿工节点
            var minerTasks = new List<Task>();
            for (int i = 0; i < 3; i++)
            {
                int minerId = i + 1;
                minerTasks.Add(Task.Run(() => MinerNode(minerId.ToString(), blockchain)));
            }
            
            // 添加监控任务
            var monitorTask = Task.Run(() => 
            {
                while (true)
                {
                    Task.Delay(30000).Wait();
                    monitor.GenerateCarbonReport();
                    monitor.AnalyzeDifficultyTrend();
                }
            });
            
            await Task.WhenAll(minerTasks);
        }
        
        static void MinerNode(string minerId, CarbonAwareBlockchain blockchain)
        {
            var random = new Random();
            
            while (true)
            {
                try
                {
                    // 1. 生成模拟逆变器数据
                    for (int i = 0; i < 5; i++)
                    {
                        var data = InverterSimulator.GenerateSimulatedData("PVK-2023-001");
                        blockchain.AddInverterData(data);
                        Task.Delay(500).Wait();
                    }
                    
                    // 2. 挖矿创建新区块
                    blockchain.MineBlock($"Miner-{minerId}");
                    
                    // 3. 随机等待
                    Task.Delay(random.Next(3000, 8000)).Wait();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"矿工 {minerId} 错误: {ex.Message}");
                    Task.Delay(5000).Wait();
                }
            }
        }
    }
}