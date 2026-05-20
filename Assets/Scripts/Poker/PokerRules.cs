using System;
using System.Collections.Generic;

public enum PokerSuit
{
    Clubs,
    Diamonds,
    Hearts,
    Spades
}

public enum PokerRank
{
    Two = 2,
    Three = 3,
    Four = 4,
    Five = 5,
    Six = 6,
    Seven = 7,
    Eight = 8,
    Nine = 9,
    Ten = 10,
    Jack = 11,
    Queen = 12,
    King = 13,
    Ace = 14
}

public enum PokerRound
{
    Preflop,
    Flop,
    Turn,
    River,
    Finished
}

public enum PokerResult
{
    None,
    PlayerWin,
    BotWin,
    Tie
}

public enum PokerHandCategory
{
    HighCard,
    Pair,
    TwoPair,
    ThreeOfAKind,
    Straight,
    Flush,
    FullHouse,
    FourOfAKind,
    StraightFlush
}

public readonly struct PokerCard
{
    public PokerCard(PokerRank rank, PokerSuit suit)
    {
        Rank = rank;
        Suit = suit;
    }

    public PokerRank Rank { get; }
    public PokerSuit Suit { get; }

    public override string ToString()
    {
        return $"{RankToText((int)Rank)}{SuitToText(Suit)}";
    }

    private static string RankToText(int rank)
    {
        switch (rank)
        {
            case 14:
                return "A";
            case 13:
                return "K";
            case 12:
                return "Q";
            case 11:
                return "J";
            case 10:
                return "T";
            default:
                return rank.ToString();
        }
    }

    private static string SuitToText(PokerSuit suit)
    {
        switch (suit)
        {
            case PokerSuit.Clubs:
                return "C";
            case PokerSuit.Diamonds:
                return "D";
            case PokerSuit.Hearts:
                return "H";
            case PokerSuit.Spades:
                return "S";
            default:
                return "?";
        }
    }
}

public readonly struct PokerHandValue : IComparable<PokerHandValue>
{
    private readonly int[] tieBreakers;

    public PokerHandValue(PokerHandCategory category, params int[] tieBreakers)
    {
        Category = category;
        this.tieBreakers = tieBreakers ?? Array.Empty<int>();
    }

    public PokerHandCategory Category { get; }

    public string DisplayName
    {
        get
        {
            switch (Category)
            {
                case PokerHandCategory.StraightFlush:
                    return "Straight Flush";
                case PokerHandCategory.FourOfAKind:
                    return "Four of a Kind";
                case PokerHandCategory.FullHouse:
                    return "Full House";
                case PokerHandCategory.Flush:
                    return "Flush";
                case PokerHandCategory.Straight:
                    return "Straight";
                case PokerHandCategory.ThreeOfAKind:
                    return "Three of a Kind";
                case PokerHandCategory.TwoPair:
                    return "Two Pair";
                case PokerHandCategory.Pair:
                    return "Pair";
                default:
                    return "High Card";
            }
        }
    }

    public int CompareTo(PokerHandValue other)
    {
        int categoryComparison = Category.CompareTo(other.Category);

        if (categoryComparison != 0)
        {
            return categoryComparison;
        }

        int maxLength = Math.Max(tieBreakers?.Length ?? 0, other.tieBreakers?.Length ?? 0);

        for (int i = 0; i < maxLength; i++)
        {
            int left = tieBreakers != null && i < tieBreakers.Length ? tieBreakers[i] : 0;
            int right = other.tieBreakers != null && i < other.tieBreakers.Length ? other.tieBreakers[i] : 0;

            if (left != right)
            {
                return left.CompareTo(right);
            }
        }

        return 0;
    }
}

public sealed class OfflinePokerMatch
{
    private readonly int startingChips;
    private readonly int ante;
    private readonly int betSize;
    private readonly Random random;
    private readonly List<PokerCard> deck = new List<PokerCard>(52);
    private readonly List<PokerCard> playerHoleCards = new List<PokerCard>(2);
    private readonly List<PokerCard> botHoleCards = new List<PokerCard>(2);
    private readonly List<PokerCard> communityCards = new List<PokerCard>(5);
    private readonly List<string> logEntries = new List<string>();

    public OfflinePokerMatch(int startingChips = 100, int ante = 5, int betSize = 10)
    {
        this.startingChips = Math.Max(30, startingChips);
        this.ante = Math.Max(1, ante);
        this.betSize = Math.Max(1, betSize);
        random = new Random(Environment.TickCount);
        StartNewHand();
    }

    public IReadOnlyList<PokerCard> PlayerHoleCards => playerHoleCards;
    public IReadOnlyList<PokerCard> BotHoleCards => botHoleCards;
    public IReadOnlyList<PokerCard> CommunityCards => communityCards;
    public IReadOnlyList<string> LogEntries => logEntries;
    public int PlayerChips { get; private set; }
    public int BotChips { get; private set; }
    public int Pot { get; private set; }
    public int PlayerRoundBet { get; private set; }
    public int BotRoundBet { get; private set; }
    public int BetSize => betSize;
    public int PlayerToCall => Math.Max(0, BotRoundBet - PlayerRoundBet);
    public PokerRound Round { get; private set; }
    public PokerResult Result { get; private set; }
    public PokerHandValue PlayerBestHand { get; private set; }
    public PokerHandValue BotBestHand { get; private set; }
    public string StatusText { get; private set; }
    public bool IsComplete => Round == PokerRound.Finished;
    public bool CanAct => !IsComplete;

    public void StartNewHand()
    {
        PlayerChips = startingChips;
        BotChips = startingChips;
        Pot = 0;
        PlayerRoundBet = 0;
        BotRoundBet = 0;
        Result = PokerResult.None;
        PlayerBestHand = default;
        BotBestHand = default;
        Round = PokerRound.Preflop;
        StatusText = "New hand. Antes are posted.";

        deck.Clear();
        playerHoleCards.Clear();
        botHoleCards.Clear();
        communityCards.Clear();
        logEntries.Clear();

        BuildDeck();
        ShuffleDeck();
        CommitPlayer(ante);
        CommitBot(ante);
        ResetRoundBets();

        playerHoleCards.Add(Draw());
        botHoleCards.Add(Draw());
        playerHoleCards.Add(Draw());
        botHoleCards.Add(Draw());

        AddLog("You sit at the table. Both players post ante.");
        AddLog("Two cards are dealt to each player.");
    }

    public void PlayerCheckOrCall()
    {
        if (!CanAct)
        {
            return;
        }

        int toCall = PlayerToCall;

        if (toCall > 0)
        {
            CommitPlayer(toCall);
            AddLog($"You call {toCall}.");
            AdvanceRound();
            return;
        }

        AddLog("You check.");
        BotActAfterPlayerCheck();
    }

    public void PlayerBetOrRaise()
    {
        if (!CanAct)
        {
            return;
        }

        int toCall = PlayerToCall;
        int total = toCall + betSize;
        CommitPlayer(total);

        if (toCall > 0)
        {
            AddLog($"You call {toCall} and raise {betSize}.");
        }
        else
        {
            AddLog($"You bet {betSize}.");
        }

        BotRespondToPlayerBet();
    }

    public void PlayerFold()
    {
        if (!CanAct)
        {
            return;
        }

        AddLog("You fold.");
        FinishByFold(PokerResult.BotWin, "Bot wins the pot.");
    }

    private void BotActAfterPlayerCheck()
    {
        if (ShouldBotBet())
        {
            CommitBot(betSize);
            StatusText = $"Bot bets {betSize}. Call or fold.";
            AddLog($"Bot bets {betSize}.");
            return;
        }

        AddLog("Bot checks.");
        AdvanceRound();
    }

    private void BotRespondToPlayerBet()
    {
        int toCall = Math.Max(0, PlayerRoundBet - BotRoundBet);

        if (toCall <= 0 || ShouldBotContinue(toCall))
        {
            CommitBot(toCall);
            AddLog($"Bot calls {toCall}.");
            AdvanceRound();
            return;
        }

        AddLog("Bot folds.");
        FinishByFold(PokerResult.PlayerWin, "You win the pot.");
    }

    private void AdvanceRound()
    {
        ResetRoundBets();

        switch (Round)
        {
            case PokerRound.Preflop:
                DealCommunityCards(3);
                Round = PokerRound.Flop;
                StatusText = "Flop dealt.";
                AddLog("Flop is dealt.");
                break;
            case PokerRound.Flop:
                DealCommunityCards(1);
                Round = PokerRound.Turn;
                StatusText = "Turn dealt.";
                AddLog("Turn is dealt.");
                break;
            case PokerRound.Turn:
                DealCommunityCards(1);
                Round = PokerRound.River;
                StatusText = "River dealt.";
                AddLog("River is dealt.");
                break;
            case PokerRound.River:
                ResolveShowdown();
                break;
        }
    }

    private void ResolveShowdown()
    {
        List<PokerCard> playerCards = new List<PokerCard>(7);
        List<PokerCard> botCards = new List<PokerCard>(7);
        playerCards.AddRange(playerHoleCards);
        playerCards.AddRange(communityCards);
        botCards.AddRange(botHoleCards);
        botCards.AddRange(communityCards);

        PlayerBestHand = PokerHandEvaluator.EvaluateBest(playerCards);
        BotBestHand = PokerHandEvaluator.EvaluateBest(botCards);

        int comparison = PlayerBestHand.CompareTo(BotBestHand);

        if (comparison > 0)
        {
            PlayerChips += Pot;
            Result = PokerResult.PlayerWin;
            StatusText = $"You win with {PlayerBestHand.DisplayName}.";
            AddLog(StatusText);
        }
        else if (comparison < 0)
        {
            BotChips += Pot;
            Result = PokerResult.BotWin;
            StatusText = $"Bot wins with {BotBestHand.DisplayName}.";
            AddLog(StatusText);
        }
        else
        {
            int split = Pot / 2;
            PlayerChips += split;
            BotChips += Pot - split;
            Result = PokerResult.Tie;
            StatusText = $"Split pot. Both show {PlayerBestHand.DisplayName}.";
            AddLog(StatusText);
        }

        Pot = 0;
        Round = PokerRound.Finished;
    }

    private void FinishByFold(PokerResult result, string status)
    {
        Result = result;

        if (result == PokerResult.PlayerWin)
        {
            PlayerChips += Pot;
        }
        else if (result == PokerResult.BotWin)
        {
            BotChips += Pot;
        }

        Pot = 0;
        Round = PokerRound.Finished;
        StatusText = status;
        AddLog(status);
    }

    private bool ShouldBotBet()
    {
        float strength = EstimateBotStrength();
        return random.NextDouble() < 0.25f + strength * 0.35f;
    }

    private bool ShouldBotContinue(int toCall)
    {
        float strength = EstimateBotStrength();
        float potPressure = Pot <= 0 ? 0f : Math.Min(0.35f, (float)toCall / Pot);
        return random.NextDouble() < 0.35f + strength * 0.55f - potPressure;
    }

    private float EstimateBotStrength()
    {
        if (communityCards.Count >= 3)
        {
            List<PokerCard> cards = new List<PokerCard>(7);
            cards.AddRange(botHoleCards);
            cards.AddRange(communityCards);
            PokerHandValue value = PokerHandEvaluator.EvaluateBest(cards);
            return Math.Min(1f, ((int)value.Category + 1) / 9f);
        }

        int first = (int)botHoleCards[0].Rank;
        int second = (int)botHoleCards[1].Rank;
        bool pair = first == second;
        bool suited = botHoleCards[0].Suit == botHoleCards[1].Suit;
        int high = Math.Max(first, second);
        float strength = high / 14f * 0.45f;

        if (pair)
        {
            strength += 0.35f;
        }

        if (suited)
        {
            strength += 0.1f;
        }

        return Math.Min(1f, strength);
    }

    private void CommitPlayer(int amount)
    {
        int committed = Math.Min(Math.Max(0, amount), PlayerChips);
        PlayerChips -= committed;
        PlayerRoundBet += committed;
        Pot += committed;
    }

    private void CommitBot(int amount)
    {
        int committed = Math.Min(Math.Max(0, amount), BotChips);
        BotChips -= committed;
        BotRoundBet += committed;
        Pot += committed;
    }

    private void ResetRoundBets()
    {
        PlayerRoundBet = 0;
        BotRoundBet = 0;
    }

    private void DealCommunityCards(int count)
    {
        for (int i = 0; i < count; i++)
        {
            communityCards.Add(Draw());
        }
    }

    private void BuildDeck()
    {
        foreach (PokerSuit suit in Enum.GetValues(typeof(PokerSuit)))
        {
            for (int rank = (int)PokerRank.Two; rank <= (int)PokerRank.Ace; rank++)
            {
                deck.Add(new PokerCard((PokerRank)rank, suit));
            }
        }
    }

    private void ShuffleDeck()
    {
        for (int i = deck.Count - 1; i > 0; i--)
        {
            int swapIndex = random.Next(i + 1);
            PokerCard card = deck[i];
            deck[i] = deck[swapIndex];
            deck[swapIndex] = card;
        }
    }

    private PokerCard Draw()
    {
        PokerCard card = deck[deck.Count - 1];
        deck.RemoveAt(deck.Count - 1);
        return card;
    }

    private void AddLog(string message)
    {
        logEntries.Add(message);

        if (logEntries.Count > 8)
        {
            logEntries.RemoveAt(0);
        }
    }
}

public static class PokerHandEvaluator
{
    public static PokerHandValue EvaluateBest(IReadOnlyList<PokerCard> cards)
    {
        if (cards == null || cards.Count < 5)
        {
            return new PokerHandValue(PokerHandCategory.HighCard, 0);
        }

        PokerHandValue best = default;
        bool hasBest = false;

        for (int a = 0; a < cards.Count - 4; a++)
        {
            for (int b = a + 1; b < cards.Count - 3; b++)
            {
                for (int c = b + 1; c < cards.Count - 2; c++)
                {
                    for (int d = c + 1; d < cards.Count - 1; d++)
                    {
                        for (int e = d + 1; e < cards.Count; e++)
                        {
                            PokerHandValue value = EvaluateFive(cards[a], cards[b], cards[c], cards[d], cards[e]);

                            if (!hasBest || value.CompareTo(best) > 0)
                            {
                                best = value;
                                hasBest = true;
                            }
                        }
                    }
                }
            }
        }

        return best;
    }

    private static PokerHandValue EvaluateFive(PokerCard first, PokerCard second, PokerCard third, PokerCard fourth, PokerCard fifth)
    {
        PokerCard[] hand = { first, second, third, fourth, fifth };
        int[] counts = new int[15];
        int[] ranks = new int[5];
        bool flush = true;

        for (int i = 0; i < hand.Length; i++)
        {
            int rank = (int)hand[i].Rank;
            counts[rank]++;
            ranks[i] = rank;

            if (hand[i].Suit != hand[0].Suit)
            {
                flush = false;
            }
        }

        Array.Sort(ranks);
        Array.Reverse(ranks);
        int straightHigh = GetStraightHigh(counts);

        if (flush && straightHigh > 0)
        {
            return new PokerHandValue(PokerHandCategory.StraightFlush, straightHigh);
        }

        int four = FindRankWithCount(counts, 4);

        if (four > 0)
        {
            return new PokerHandValue(PokerHandCategory.FourOfAKind, four, FindHighestExcept(counts, four));
        }

        int three = FindRankWithCount(counts, 3);
        int pair = FindRankWithCount(counts, 2);

        if (three > 0 && pair > 0)
        {
            return new PokerHandValue(PokerHandCategory.FullHouse, three, pair);
        }

        if (flush)
        {
            return new PokerHandValue(PokerHandCategory.Flush, ranks);
        }

        if (straightHigh > 0)
        {
            return new PokerHandValue(PokerHandCategory.Straight, straightHigh);
        }

        if (three > 0)
        {
            return new PokerHandValue(PokerHandCategory.ThreeOfAKind, Combine(three, GetKickers(counts, three, 2)));
        }

        int highPair = FindRankWithCount(counts, 2);
        int lowPair = FindRankWithCount(counts, 2, highPair);

        if (highPair > 0 && lowPair > 0)
        {
            return new PokerHandValue(PokerHandCategory.TwoPair, highPair, lowPair, FindHighestExcept(counts, highPair, lowPair));
        }

        if (highPair > 0)
        {
            return new PokerHandValue(PokerHandCategory.Pair, Combine(highPair, GetKickers(counts, highPair, 3)));
        }

        return new PokerHandValue(PokerHandCategory.HighCard, ranks);
    }

    private static int GetStraightHigh(int[] counts)
    {
        bool[] present = new bool[15];

        for (int rank = 2; rank <= 14; rank++)
        {
            present[rank] = counts[rank] > 0;
        }

        present[1] = present[14];

        for (int high = 14; high >= 5; high--)
        {
            bool hasStraight = true;

            for (int offset = 0; offset < 5; offset++)
            {
                if (!present[high - offset])
                {
                    hasStraight = false;
                    break;
                }
            }

            if (hasStraight)
            {
                return high;
            }
        }

        return 0;
    }

    private static int FindRankWithCount(int[] counts, int targetCount, int excludeRank = 0)
    {
        for (int rank = 14; rank >= 2; rank--)
        {
            if (rank != excludeRank && counts[rank] == targetCount)
            {
                return rank;
            }
        }

        return 0;
    }

    private static int FindHighestExcept(int[] counts, params int[] excludedRanks)
    {
        for (int rank = 14; rank >= 2; rank--)
        {
            if (counts[rank] <= 0)
            {
                continue;
            }

            bool excluded = false;

            for (int i = 0; i < excludedRanks.Length; i++)
            {
                if (rank == excludedRanks[i])
                {
                    excluded = true;
                    break;
                }
            }

            if (!excluded)
            {
                return rank;
            }
        }

        return 0;
    }

    private static int[] GetKickers(int[] counts, int excludedRank, int count)
    {
        int[] kickers = new int[count];
        int index = 0;

        for (int rank = 14; rank >= 2 && index < count; rank--)
        {
            if (rank != excludedRank && counts[rank] > 0)
            {
                kickers[index++] = rank;
            }
        }

        return kickers;
    }

    private static int[] Combine(int first, int[] rest)
    {
        int[] combined = new int[rest.Length + 1];
        combined[0] = first;

        for (int i = 0; i < rest.Length; i++)
        {
            combined[i + 1] = rest[i];
        }

        return combined;
    }
}
