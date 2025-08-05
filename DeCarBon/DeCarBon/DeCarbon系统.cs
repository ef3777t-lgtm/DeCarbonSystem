using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.IO;
using Newtonsoft.Json;
using System.Linq;
using System.Numerics;

namespace PerovskiteCarbonCurrency
{
    /// <summary>
    /// 钙钛矿碳减排数字货币系统核心
    /// 版权所有 (C) 2025 [您的姓名/公司名]
    /// </summary>
    
    // ====================== 数据结构定义 ======================
    
    /// <summary>
    /// 钙钛矿组件生产数据
    /// </summary>
    public struct PerovskiteProductionData
    {
        public string PanelId { get; set; }          // 组件唯一ID
        public DateTime ProductionDate { get; set; } // 生产日期
        public double Efficiency { get; set; }       // 转换效率(%)
        public double Size { get; set; }             // 组件尺寸(m²)
        public string Manufacturer { get; set; }     // 制造商
        public double CarbonReduction { get; set; }   // 碳减排量(kgCO2e/kWh)
        public int Lifetime { get; set; }            // 预期寿命(年)
        
        /// <summary>
        /// 计算组件生命周期总碳减排量
        /// </summary>
        public double CalculateLifetimeReduction(double solarIrradiance = 1500)
        {
            double annualEnergy = Efficiency/100 * Size * solarIrradiance;
            return annualEnergy * CarbonReduction * Lifetime;
        }
    }

    /// <summary>
    /// 数字货币交易记录
    /// </summary>
    public class CarbonTransaction
    {
        public string TransactionId { get; }          // 交易ID
        public string Sender { get; set; }            // 发送方
        public string Receiver { get; set; }           // 接收方
        public decimal Amount { get; set; }            // 交易数量
        public DateTime Timestamp { get; set; }        // 时间戳
        public string PanelId { get; set; }           // 关联组件ID
        public string Signature { get; set; }          // 数字签名
        
        public CarbonTransaction()
        {
            TransactionId = GenerateHash(Guid.NewGuid().ToString());
            Timestamp = DateTime.UtcNow;
        }
        
        private static string GenerateHash(string input)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
                return BitConverter.ToString(bytes).Replace("-", "");
            }
        }
    }

    /// <summary>
    /// 区块链区块结构
    /// </summary>
    public class Block
    {
        public int Index { get; set; }                // 区块高度
        public DateTime Timestamp { get; set; }        // 时间戳
        public string PreviousHash { get; set; }       // 前一区块哈希
        public string Hash { get; set; }              // 当前区块哈希
        public List<CarbonTransaction> Transactions { get; set; } // 交易列表
        public int Nonce { get; set; }                // 工作量证明随机数
        public string Miner { get; set; }             // 矿工地址
        public double TotalCarbonReduction { get; set; } // 区块总碳减排量
        
        public Block()
        {
            Transactions = new List<CarbonTransaction>();
            Timestamp = DateTime.UtcNow;
        }
        
        public string CalculateHash()
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                string rawData = $"{Index}{Timestamp:O}{PreviousHash}{Nonce}";
                foreach (var tx in Transactions)
                {
                    rawData += tx.TransactionId;
                }
                
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(rawData));
                return BitConverter.ToString(bytes).Replace("-", "");
            }
        }
    }

    // ====================== 核心算法模块 ======================
    
    /// <summary>
    /// 碳减排数字货币算法核心
    /// </summary>
    public static class CarbonCurrencyAlgorithm
    {
        /// <summary>
        /// 计算钙钛矿组件的初始货币发行量
        /// </summary>
        /// <param name="data">生产数据</param>
        /// <returns>应发行的数字货币量</returns>
        public static decimal CalculateInitialIssuance(PerovskiteProductionData data)
        {
            // 基于碳减排量、效率和寿命的综合算法
            double reduction = data.CalculateLifetimeReduction();
            double efficiencyFactor = Math.Pow(data.Efficiency / 20.0, 1.5);
            double lifetimeFactor = 1 + Math.Log(data.Lifetime) / 10;
            
            // 标准化为数字货币单位 (1单位 = 100kg CO2减排)
            decimal issuance = (decimal)(reduction * efficiencyFactor * lifetimeFactor / 100);
            
            // 应用对数曲线防止高值过度增长
            return ScaleIssuance(issuance);
        }
        
        private static decimal ScaleIssuance(decimal rawValue)
        {
            // 使用对数函数平滑发行曲线
            double scaled = Math.Log10((double)rawValue + 1) * 100;
            return Math.Round((decimal)scaled, 4);
        }
        
        /// <summary>
        /// 验证交易有效性
        /// </summary>
        public static bool ValidateTransaction(CarbonTransaction transaction, 
                                              Dictionary<string, decimal> balances)
        {
            // 基础验证
            if (transaction == null) return false;
            if (string.IsNullOrEmpty(transaction.Sender)) return false;
            if (string.IsNullOrEmpty(transaction.Receiver)) return false;
            if (transaction.Amount <= 0) return false;
            
            // 签名验证 (使用ECDsa实现)
            if (!VerifySignature(transaction)) return false;
            
            // 余额验证
            if (balances.ContainsKey(transaction.Sender) && 
                balances[transaction.Sender] < transaction.Amount)
            {
                return false;
            }
            
            return true;
        }
        
        public static bool VerifySignature(CarbonTransaction transaction)
        {
            // 实际应用中应使用ECDsa验证
            // 此处为简化版实现
            return !string.IsNullOrEmpty(transaction.Signature) && 
                   transaction.Signature.Length == 128;
        }
        
        /// <summary>
        /// 工作量证明算法
        /// </summary>
        public static (string hash, int nonce) ProofOfWork(string lastHash, List<CarbonTransaction> transactions, double carbonReduction)
        {
            int difficulty = CalculateDifficulty(carbonReduction);
            int nonce = 0;
            string hash;
            
            do {
                nonce++;
                string rawData = $"{lastHash}{nonce}";
                foreach (var tx in transactions)
                {
                    rawData += tx.TransactionId;
                }
                
                using (SHA256 sha256 = SHA256.Create())
                {
                    byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(rawData));
                    hash = BitConverter.ToString(bytes).Replace("-", "");
                }
            } while (!hash.StartsWith(new string('0', difficulty)));
            
            return (hash, nonce);
        }
        
        private static int CalculateDifficulty(double carbonReduction)
        {
            // 基于碳减排量的动态难度调整
            double baseDifficulty = 4;
            double adjustment = Math.Log10(carbonReduction + 1);
            return (int)Math.Max(2, Math.Min(8, baseDifficulty + adjustment));
        }
    }
    
    // ====================== 系统核心模块 ======================
    
    /// <summary>
    /// 区块链实现
    /// </summary>
    public class CarbonBlockchain
    {
        public List<Block> Chain { get; set; }
        public List<CarbonTransaction> PendingTransactions { get; set; }
        public Dictionary<string, PerovskiteProductionData> PanelRegistry { get; set; }
        public Dictionary<string, decimal> Balances { get;  set; }
        
        public CarbonBlockchain()
        {
            Chain = new List<Block>();
            PendingTransactions = new List<CarbonTransaction>();
            PanelRegistry = new Dictionary<string, PerovskiteProductionData>();
            Balances = new Dictionary<string, decimal>();
            
            // 创建创世区块
            CreateGenesisBlock();
        }
        
        private void CreateGenesisBlock()
        {
            Block genesis = new Block
            {
                Index = 0,
                Timestamp = DateTime.UtcNow,
                PreviousHash = "0",
                Transactions = new List<CarbonTransaction>(),
                Miner = "system",
                TotalCarbonReduction = 0
            };
            
            genesis.Hash = genesis.CalculateHash();
            Chain.Add(genesis);
        }
        
        /// <summary>
        /// 注册钙钛矿组件并发行初始货币
        /// </summary>
        public void RegisterPerovskitePanel(PerovskiteProductionData data, string owner)
        {
            if (PanelRegistry.ContainsKey(data.PanelId))
                throw new InvalidOperationException("Panel already registered");
            
            PanelRegistry[data.PanelId] = data;
            
            // 计算初始发行量
            decimal issuance = CarbonCurrencyAlgorithm.CalculateInitialIssuance(data);
            
            // 创建发行交易
            var transaction = new CarbonTransaction
            {
                Sender = "system",
                Receiver = owner,
                Amount = issuance,
                PanelId = data.PanelId
            };
            
            // 更新余额
            if (!Balances.ContainsKey(owner)) Balances[owner] = 0;
            Balances[owner] += issuance;
            
            // 添加到待处理交易
            PendingTransactions.Add(transaction);
        }
        
        /// <summary>
        /// 添加新交易
        /// </summary>
        public bool AddTransaction(CarbonTransaction transaction)
        {
            if (!CarbonCurrencyAlgorithm.ValidateTransaction(transaction, Balances))
                return false;
                
            PendingTransactions.Add(transaction);
            return true;
        }
        
        /// <summary>
        /// 挖矿创建新区块
        /// </summary>
        public void MineBlock(string minerAddress)
        {
            if (PendingTransactions.Count == 0)
                throw new InvalidOperationException("No transactions to mine");
            
            double totalCarbon = CalculateBlockCarbonReduction();
            
            var lastBlock = Chain.Last();
            var newBlock = new Block
            {
                Index = lastBlock.Index + 1,
                PreviousHash = lastBlock.Hash,
                Transactions = new List<CarbonTransaction>(PendingTransactions),
                TotalCarbonReduction = totalCarbon,
                Miner = minerAddress
            };
            
            // 工作量证明
            (newBlock.Hash, newBlock.Nonce) = 
                CarbonCurrencyAlgorithm.ProofOfWork(lastBlock.Hash, PendingTransactions, totalCarbon);
            
            // 添加区块到链
            Chain.Add(newBlock);
            
            // 更新余额
            UpdateBalances(newBlock.Transactions);
            
            // 清空待处理交易
            PendingTransactions.Clear();
            
            // 矿工奖励
            decimal reward = CalculateMiningReward(newBlock.Index);
            Balances[minerAddress] = Balances.GetValueOrDefault(minerAddress) + reward;
        }
        
        private double CalculateBlockCarbonReduction()
        {
            double total = 0;
            foreach (var tx in PendingTransactions)
            {
                if (!string.IsNullOrEmpty(tx.PanelId) && PanelRegistry.ContainsKey(tx.PanelId))
                {
                    total += PanelRegistry[tx.PanelId].CalculateLifetimeReduction();
                }
            }
            return total;
        }
        
        private void UpdateBalances(List<CarbonTransaction> transactions)
        {
            foreach (var tx in transactions)
            {
                if (tx.Sender != "system")
                {
                    Balances[tx.Sender] -= tx.Amount;
                }
                Balances[tx.Receiver] = Balances.GetValueOrDefault(tx.Receiver) + tx.Amount;
            }
        }
        
        private decimal CalculateMiningReward(int blockHeight)
        {
            // 每210000区块减半
            int halvingInterval = 210000;
            int halvings = blockHeight / halvingInterval;
            
            // 初始奖励50，最多减半64次
            decimal reward = 50.0m;
            for (int i = 0; i < Math.Min(halvings, 64); i++)
            {
                reward /= 2;
            }
            
            return reward;
        }
        
        /// <summary>
        /// 验证区块链完整性
        /// </summary>
        public bool ValidateChain()
        {
            for (int i = 1; i < Chain.Count; i++)
            {
                Block current = Chain[i];
                Block previous = Chain[i - 1];
                
                // 验证哈希链接
                if (current.PreviousHash != previous.Hash)
                    return false;
                
                // 验证区块哈希
                if (current.Hash != current.CalculateHash())
                    return false;
                
                // 验证交易签名
                foreach (var tx in current.Transactions)
                {
                    if (!CarbonCurrencyAlgorithm.VerifySignature(tx))
                        return false;
                }
            }
            return true;
        }
    }
    
    // ====================== 网络与API模块 ======================
    
    /// <summary>
    /// P2P网络节点
    /// </summary>
    public class NetworkNode
    {
        public string NodeId { get; }
        public CarbonBlockchain Blockchain { get; }
        public List<string> PeerNodes { get; }
        
        public NetworkNode()
        {
            NodeId = GenerateNodeId();
            Blockchain = new CarbonBlockchain();
            PeerNodes = new List<string>();
        }
        
        private string GenerateNodeId()
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(Guid.NewGuid().ToString()));
                return "NODE" + BitConverter.ToString(bytes).Replace("-", "").Substring(0, 12);
            }
        }
        
        public void ConnectToPeer(string peerAddress)
        {
            if (!PeerNodes.Contains(peerAddress))
            {
                PeerNodes.Add(peerAddress);
                SyncBlockchain();
            }
        }
        
        public void SyncBlockchain()
        {
            // 简化实现 - 实际应选择最长有效链
            foreach (var peer in PeerNodes)
            {
                // 模拟从网络获取区块链
                // CarbonBlockchain peerChain = NetworkService.GetBlockchain(peer);
                // if (peerChain.Chain.Count > Blockchain.Chain.Count && peerChain.ValidateChain())
                // {
                //    Blockchain = peerChain;
                // }
            }
        }
        
        public void BroadcastTransaction(CarbonTransaction transaction)
        {
            // 模拟广播到所有节点
            foreach (var peer in PeerNodes)
            {
                // NetworkService.SendTransaction(peer, transaction);
            }
        }
        
        public void BroadcastBlock(Block block)
        {
            // 模拟广播新区块
            foreach (var peer in PeerNodes)
            {
                // NetworkService.SendBlock(peer, block);
            }
        }
    }
    
    /// <summary>
    /// REST API服务
    /// </summary>
    public class CarbonCurrencyApi
    {
        private readonly NetworkNode _node;
        
        public CarbonCurrencyApi(NetworkNode node)
        {
            _node = node;
        }
        
        // API方法示例 (实际应使用ASP.NET Core实现)
        public string GetBalance(string address)
        {
            return _node.Blockchain.Balances.GetValueOrDefault(address, 0).ToString("F4");
        }
        
        public string RegisterPanel(PerovskiteProductionData data, string owner)
        {
            try
            {
                _node.Blockchain.RegisterPerovskitePanel(data, owner);
                return $"Panel {data.PanelId} registered. {data.CalculateLifetimeReduction():F2} kg CO2 reduction";
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }
        
        public string CreateTransaction(string sender, string receiver, decimal amount, string panelId = null)
        {
            var transaction = new CarbonTransaction
            {
                Sender = sender,
                Receiver = receiver,
                Amount = amount,
                PanelId = panelId,
                Signature = GenerateSignature(sender) // 实际应使用私钥签名
            };
            
            if (_node.Blockchain.AddTransaction(transaction))
            {
                _node.BroadcastTransaction(transaction);
                return $"Transaction created: {transaction.TransactionId}";
            }
            return "Transaction failed";
        }
        
        private string GenerateSignature(string address)
        {
            // 实际应使用ECDsa签名
            using (SHA512 sha512 = SHA512.Create())
            {
                byte[] bytes = sha512.ComputeHash(Encoding.UTF8.GetBytes(address + DateTime.UtcNow.Ticks));
                return BitConverter.ToString(bytes).Replace("-", "");
            }
        }
    }
    
    // ====================== 数据存储模块 ======================
    
    /// <summary>
    /// 区块链数据存储
    /// </summary>
    public class BlockchainStorage
    {
        private const string ChainFile = "blockchain.dat";
        private const string PanelRegistryFile = "panels.dat";
        private const string BalancesFile = "balances.dat";
        
        public void SaveBlockchain(CarbonBlockchain blockchain)
        {
            var settings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                TypeNameHandling = TypeNameHandling.Auto
            };
            
            string json = JsonConvert.SerializeObject(blockchain.Chain, settings);
            File.WriteAllText(ChainFile, json);
            
            json = JsonConvert.SerializeObject(blockchain.PanelRegistry, settings);
            File.WriteAllText(PanelRegistryFile, json);
            
            json = JsonConvert.SerializeObject(blockchain.Balances, settings);
            File.WriteAllText(BalancesFile, json);
        }
        
        public CarbonBlockchain LoadBlockchain()
        {
            var blockchain = new CarbonBlockchain();
            
            if (File.Exists(ChainFile))
            {
                var settings = new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.Auto
                };
                
                string json = File.ReadAllText(ChainFile);
                blockchain.Chain = JsonConvert.DeserializeObject<List<Block>>(json, settings);
                
                json = File.ReadAllText(PanelRegistryFile);
                blockchain.PanelRegistry = JsonConvert.DeserializeObject<Dictionary<string, PerovskiteProductionData>>(json, settings);
                
                json = File.ReadAllText(BalancesFile);
                blockchain.Balances = JsonConvert.DeserializeObject<Dictionary<string, decimal>>(json, settings);
            }
            
            return blockchain;
        }
    }
    
    // ====================== 智能合约模块 ======================
    
    /// <summary>
    /// 碳减排智能合约系统
    /// </summary>
    public class CarbonSmartContractSystem
    {
        public Dictionary<string, Func<object[], object>> Contracts { get; }
        
        public CarbonSmartContractSystem()
        {
            Contracts = new Dictionary<string, Func<object[], object>>();
            RegisterDefaultContracts();
        }
        
        private void RegisterDefaultContracts()
        {
            // 碳抵消合约
            RegisterContract("CarbonOffset", parameters => 
            {
                if (parameters.Length != 3) 
                    throw new ArgumentException("Invalid parameters");
                
                string user = (string)parameters[0];
                decimal amount = (decimal)parameters[1];
                double carbonAmount = (double)parameters[2];
                
                // 1单位货币抵消100kg CO2
                double requiredUnits = carbonAmount / 100.0;
                
                if (amount >= (decimal)requiredUnits)
                {
                    return $"Offset {carbonAmount}kg CO2 with {amount} CC tokens";
                }
                return "Insufficient tokens for offset";
            });
            
            // 碳交易市场合约
            RegisterContract("CreateMarketListing", parameters =>
            {
                if (parameters.Length != 3) 
                    throw new ArgumentException("Invalid parameters");
                
                string seller = (string)parameters[0];
                decimal amount = (decimal)parameters[1];
                double carbonAmount = (double)parameters[2];
                
                return $"Market listing created: {carbonAmount}kg CO2 for {amount} CC tokens";
            });
        }
        
        public void RegisterContract(string name, Func<object[], object> contractFunction)
        {
            Contracts[name] = contractFunction;
        }
        
        public object ExecuteContract(string name, params object[] parameters)
        {
            if (Contracts.TryGetValue(name, out var contract))
            {
                return contract(parameters);
            }
            throw new InvalidOperationException($"Contract '{name}' not found");
        }
    }
    
    // ====================== 用户界面模块 ======================
    
    /// <summary>
    /// 命令行界面
    /// </summary>
    public class CommandLineInterface
    {
        private readonly CarbonCurrencyApi _api;
        private readonly CarbonSmartContractSystem _contractSystem;
        
        public CommandLineInterface(CarbonCurrencyApi api, CarbonSmartContractSystem contractSystem)
        {
            _api = api;
            _contractSystem = contractSystem;
        }
        
        public void Run()
        {
            Console.WriteLine("=== 钙钛矿碳减排数字货币系统 ===");
            Console.WriteLine("1. 注册钙钛矿组件");
            Console.WriteLine("2. 查询余额");
            Console.WriteLine("3. 创建交易");
            Console.WriteLine("4. 执行智能合约");
            Console.WriteLine("5. 显示区块链信息");
            Console.WriteLine("6. 退出");
            
            while (true)
            {
                Console.Write("> ");
                string input = Console.ReadLine();
                
                switch (input)
                {
                    case "1":
                        RegisterPanel();
                        break;
                    case "2":
                        CheckBalance();
                        break;
                    case "3":
                        CreateTransaction();
                        break;
                    case "4":
                        ExecuteContract();
                        break;
                    case "5":
                        ShowBlockchainInfo();
                        break;
                    case "6":
                        return;
                    default:
                        Console.WriteLine("无效选项");
                        break;
                }
            }
        }
        
        private void RegisterPanel()
        {
            Console.WriteLine("=== 注册钙钛矿组件 ===");
            
            var data = new PerovskiteProductionData();
            
            Console.Write("组件ID: ");
            data.PanelId = Console.ReadLine();
            
            Console.Write("生产日期 (yyyy-MM-dd): ");
            if (DateTime.TryParse(Console.ReadLine(), out DateTime date))
                data.ProductionDate = date;
            
            Console.Write("转换效率 (%): ");
            if (double.TryParse(Console.ReadLine(), out double eff))
                data.Efficiency = eff;
            
            Console.Write("尺寸 (m²): ");
            if (double.TryParse(Console.ReadLine(), out double size))
                data.Size = size;
            
            Console.Write("制造商: ");
            data.Manufacturer = Console.ReadLine();
            
            Console.Write("碳减排量 (kgCO2e/kWh): ");
            if (double.TryParse(Console.ReadLine(), out double cr))
                data.CarbonReduction = cr;
            
            Console.Write("预期寿命 (年): ");
            if (int.TryParse(Console.ReadLine(), out int life))
                data.Lifetime = life;
            
            Console.Write("所有者地址: ");
            string owner = Console.ReadLine();
            
            Console.WriteLine(_api.RegisterPanel(data, owner));
        }
        
        private void CheckBalance()
        {
            Console.Write("输入地址: ");
            string address = Console.ReadLine();
            Console.WriteLine($"余额: {_api.GetBalance(address)} CC");
        }
        
        private void CreateTransaction()
        {
            Console.Write("发送方: ");
            string sender = Console.ReadLine();
            
            Console.Write("接收方: ");
            string receiver = Console.ReadLine();
            
            Console.Write("数量: ");
            if (decimal.TryParse(Console.ReadLine(), out decimal amount))
            {
                Console.Write("关联组件ID (可选): ");
                string panelId = Console.ReadLine();
                
                Console.WriteLine(_api.CreateTransaction(sender, receiver, amount, panelId));
            }
        }
        
        private void ExecuteContract()
        {
            Console.WriteLine("可用合约: ");
            foreach (var name in _contractSystem.Contracts.Keys)
            {
                Console.WriteLine($"- {name}");
            }
            
            Console.Write("选择合约: ");
            string contractName = Console.ReadLine();
            
            // 简化参数输入
            Console.WriteLine("输入参数 (空格分隔): ");
            string[] parameters = Console.ReadLine().Split(' ');
            
            try
            {
                object result = _contractSystem.ExecuteContract(contractName, parameters);
                Console.WriteLine($"结果: {result}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"错误: {ex.Message}");
            }
        }
        
        private void ShowBlockchainInfo()
        {
            // 实际应访问区块链对象
            Console.WriteLine("区块链信息:");
            Console.WriteLine($"区块高度: 0");
            Console.WriteLine($"总交易数: 0");
            Console.WriteLine($"总碳减排量: 0 kg CO2");
        }
    }
    
    // ====================== 主程序入口 ======================
    
    // class Program
    // {
    //     static void Main(string[] args)
    //     {
    //         // 初始化系统组件
    //         var node = new NetworkNode();
    //         var api = new CarbonCurrencyApi(node);
    //         var contractSystem = new CarbonSmartContractSystem();
    //         
    //         // 启动命令行界面
    //         var cli = new CommandLineInterface(api, contractSystem);
    //         cli.Run();
    //     }
    // }
}