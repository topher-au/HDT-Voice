Imports System.Speech.Recognition
Imports Hearthstone_Deck_Tracker
Imports Hearthstone_Deck_Tracker.API
Imports Hearthstone_Deck_Tracker.Enums
Imports Hearthstone_Deck_Tracker.Hearthstone
Imports Hearthstone_Deck_Tracker.Hearthstone.Entities

Public Class HDTGrammarEngine
    Private handCards, boardFriendly, boardOpposing As New List(Of Entity)

    Private friendlyID As Integer = 0
    Private opposingID As Integer = 0

    Private friendlyNames As New Choices("my", "friendly")
    Private opposingNames As New Choices("enemy", "opposing", "choices")

    Private myHand As GrammarBuilder
    Private friendlyTargets As GrammarBuilder
    Private opposingTargets As GrammarBuilder

    Public Function FriendlyHand() As GrammarBuilder
        If handCards.Count > 0 Then
            Dim cardGrammar As New GrammarBuilder

            ' build grammar for cards in hand
            Dim handGrammarNames, handGrammarNumbers, handGrammarCardNumbers As New Choices
            For Each e In handCards
                Dim CardName As New String(e.Card.Name)
                Dim CardInstances = handCards.FindAll(Function(x) x.CardId = e.CardId)
                If CardInstances.Count > 1 Then ' if we have multiple cards with the same name, add a numeric identifier
                    If Not handGrammarNames.ToGrammarBuilder.DebugShowPhrases.Contains(CardName) Then
                        handGrammarNames.Add(New SemanticResultValue(CardName, e.Id))
                        handGrammarNumbers.Add(New SemanticResultValue(e.GetTag(GAME_TAG.ZONE_POSITION).ToString.Trim, e.Id))
                        handGrammarCardNumbers.Add(New SemanticResultValue("card " & e.GetTag(GAME_TAG.ZONE_POSITION).ToString.Trim, e.Id))
                    Else
                        Dim CardNum As Integer = CardInstances.IndexOf(CardInstances.Find(Function(x) x.Id = e.Id)) + 1
                        CardName &= " " & CardNum.ToString
                        handGrammarNames.Add(New SemanticResultValue(CardName, e.Id))
                        handGrammarNumbers.Add(New SemanticResultValue(e.GetTag(GAME_TAG.ZONE_POSITION).ToString.Trim, e.Id))
                        handGrammarCardNumbers.Add(New SemanticResultValue("card " & e.GetTag(GAME_TAG.ZONE_POSITION).ToString.Trim, e.Id))
                    End If
                Else
                    handGrammarNames.Add(New SemanticResultValue(CardName, e.Id))
                    handGrammarNumbers.Add(New SemanticResultValue(e.GetTag(GAME_TAG.ZONE_POSITION).ToString.Trim, e.Id))
                    handGrammarCardNumbers.Add(New SemanticResultValue("card " & e.GetTag(GAME_TAG.ZONE_POSITION).ToString.Trim, e.Id))
                End If

            Next
            cardGrammar.Append(New SemanticResultKey("card", New Choices(handGrammarNames, handGrammarNumbers, handGrammarCardNumbers)))
            Return cardGrammar
        Else
            Return New GrammarBuilder("null")
        End If
    End Function            ' Grammar for cards in hand
    Public Function FriendlyTargetGrammar() As GrammarBuilder
        ' Build the grammar for friendly minions and hero
        Dim friendlyBuilder As New GrammarBuilder ' Represents the names and numbers of minions, and the hero
        Dim friendlyChoices As New Choices

        If boardFriendly.Count > 0 Then
            Dim friendlyGrammarNames, friendlyGrammarNumbers As New Choices
            For Each e In boardFriendly
                Dim CardName As New String(e.Card.Name)
                Dim CardInstances = boardFriendly.FindAll(Function(x) x.CardId = e.CardId)

                If CardInstances.Count > 1 Then ' More than one instance of the card on the board, append a number
                    'If it's not in the grammar already, add an un-numbered minion
                    If Not friendlyGrammarNames.ToGrammarBuilder.DebugShowPhrases.Contains(CardName) Then
                        friendlyGrammarNames.Add(New SemanticResultValue(CardName, e.Id))
                        friendlyGrammarNumbers.Add(New SemanticResultValue("minion " & e.GetTag(GAME_TAG.ZONE_POSITION).ToString.Trim, e.Id))
                    End If
                    Dim CardNum As Integer = CardInstances.IndexOf(CardInstances.Find(Function(x) x.Id = e.Id)) + 1
                    CardName &= " " & CardNum.ToString

                End If
                friendlyGrammarNames.Add(New SemanticResultValue(CardName, e.Id))
                friendlyGrammarNumbers.Add(New SemanticResultValue("minion " & e.GetTag(GAME_TAG.ZONE_POSITION).ToString.Trim, e.Id))
            Next

            friendlyChoices.Add(friendlyGrammarNames, friendlyGrammarNumbers)
        End If

        If Not IsNothing(PlayerEntity) Then
            Dim friendlyHero As New Choices
            friendlyHero.Add(New SemanticResultValue("hero", PlayerEntity.Id))
            friendlyHero.Add(New SemanticResultValue("face", PlayerEntity.Id))
            friendlyChoices.Add(friendlyHero)
        End If

        friendlyBuilder.Append(New SemanticResultKey("friendly", friendlyChoices))
        Return friendlyBuilder
    End Function    ' Grammar for friendly minions and hero
    Public Function OpposingTargetGrammar() As GrammarBuilder
        ' Build grammar for opposing minions and hero
        Dim opposingBuilder As New GrammarBuilder
        Dim opposingChoices As New Choices
        If boardOpposing.Count > 0 Then

            Dim opposingGrammarNames, opposingGrammarNumbers As New Choices
            For Each e In boardOpposing
                Dim CardName As New String(e.Card.Name)
                Dim CardInstances = boardOpposing.FindAll(Function(x) x.CardId = e.CardId)
                If CardInstances.Count > 1 Then
                    If Not opposingGrammarNames.ToGrammarBuilder.DebugShowPhrases.Contains(CardName) Then
                        opposingGrammarNames.Add(New SemanticResultValue(CardName, e.Id))
                        opposingGrammarNumbers.Add(New SemanticResultValue("minion " & e.GetTag(GAME_TAG.ZONE_POSITION).ToString.Trim, e.Id))
                    End If
                    Dim CardNum As Integer = CardInstances.IndexOf(CardInstances.Find(Function(x) x.Id = e.Id)) + 1
                    CardName &= " " & CardNum.ToString
                End If
                opposingGrammarNames.Add(New SemanticResultValue(CardName, e.Id))
                opposingGrammarNumbers.Add(New SemanticResultValue("minion " & e.GetTag(GAME_TAG.ZONE_POSITION).ToString.Trim, e.Id))
            Next
            opposingChoices.Add(opposingGrammarNames, opposingGrammarNumbers)
        End If

        If Not IsNothing(OpponentEntity) Then
            Dim opposingHero As New Choices
            opposingHero.Add(New SemanticResultValue("hero", OpponentEntity.Id))
            opposingHero.Add(New SemanticResultValue("face", OpponentEntity.Id))
            opposingChoices.Add(opposingHero)
        End If

        opposingBuilder.Append(New SemanticResultKey("opposing", New Choices(opposingChoices)))
        Return opposingBuilder
    End Function    ' Grammar for opposing minions and hero
    Public Function MenuGrammar() As GrammarBuilder
        Dim menuBuilder As New GrammarBuilder
        Dim menuChoices As New Choices


        menuChoices.Add(New SemanticResultKey("menu", "play"))
        menuChoices.Add(New SemanticResultKey("menu", "casual mode"))
        menuChoices.Add(New SemanticResultKey("menu", "ranked mode"))
        menuChoices.Add(New SemanticResultKey("menu", "basic decks"))
        menuChoices.Add(New SemanticResultKey("menu", "custom decks"))
        menuChoices.Add(New SemanticResultKey("menu", "start game"))

        menuChoices.Add(New SemanticResultKey("menu", "solo"))
        menuChoices.Add(New SemanticResultKey("menu", "versus mage"))
        menuChoices.Add(New SemanticResultKey("menu", "versus hunter"))
        menuChoices.Add(New SemanticResultKey("menu", "versus warrior"))
        menuChoices.Add(New SemanticResultKey("menu", "versus shaman"))
        menuChoices.Add(New SemanticResultKey("menu", "versus druid"))
        menuChoices.Add(New SemanticResultKey("menu", "versus priest"))
        menuChoices.Add(New SemanticResultKey("menu", "versus rogue"))
        menuChoices.Add(New SemanticResultKey("menu", "versus paladin"))
        menuChoices.Add(New SemanticResultKey("menu", "versus warlock"))

        menuChoices.Add(New SemanticResultKey("menu", "arena"))
        menuChoices.Add(New SemanticResultKey("menu", "start arena"))
        menuChoices.Add(New SemanticResultKey("menu", "buy arena with gold"))
        menuChoices.Add(New Choices(New SemanticResultKey("menu", "cancel arena"),
                                    New SemanticResultKey("menu", New SemanticResultValue("OK", "cancel arena")),
                                    New SemanticResultKey("menu", New SemanticResultValue("choose", "cancel arena"))))
        menuChoices.Add(New SemanticResultKey("menu", "hero 1"))
        menuChoices.Add(New SemanticResultKey("menu", "hero 2"))
        menuChoices.Add(New SemanticResultKey("menu", "hero 3"))
        menuChoices.Add(New SemanticResultKey("menu", "card 1"))
        menuChoices.Add(New SemanticResultKey("menu", "card 2"))
        menuChoices.Add(New SemanticResultKey("menu", "card 3"))
        menuChoices.Add(New SemanticResultKey("menu", "confirm"))

        menuChoices.Add(New Choices(New SemanticResultKey("menu", "brawl"),
                                    New SemanticResultKey("menu", New SemanticResultValue("tavern brawl", "brawl"))))
        menuChoices.Add(New SemanticResultKey("menu", "start brawl"))

        menuChoices.Add(New SemanticResultKey("menu", "open packs"))
        menuChoices.Add(New SemanticResultKey("menu", "open top pack"))
        menuChoices.Add(New SemanticResultKey("menu", "open bottom pack"))
        menuChoices.Add(New SemanticResultKey("menu", "open card 1"))
        menuChoices.Add(New SemanticResultKey("menu", "open card 2"))
        menuChoices.Add(New SemanticResultKey("menu", "open card 3"))
        menuChoices.Add(New SemanticResultKey("menu", "open card 4"))
        menuChoices.Add(New SemanticResultKey("menu", "open card 5"))
        menuChoices.Add(New SemanticResultKey("menu", "done"))

        menuChoices.Add(New SemanticResultKey("menu", "quest log"))

        menuChoices.Add(New SemanticResultKey("menu", "cancel"))
        menuChoices.Add(New SemanticResultKey("menu", "back"))

        Dim deckGrammar As New GrammarBuilder
        Dim deckChoices As New Choices
        deckGrammar.Append(New SemanticResultKey("menu", "deck"))
        For i = 1 To 9
            deckChoices.Add(New SemanticResultKey("deck", i.ToString))
        Next
        deckGrammar.Append(deckChoices)
        menuChoices.Add(deckGrammar)
        menuBuilder.Append(menuChoices)

        Return menuBuilder
    End Function
    Public Function MulliganGrammar() As GrammarBuilder

        Dim mulliganBuilder As New GrammarBuilder
        Dim mulliganChoices As New Choices

        mulliganBuilder.Append(New SemanticResultKey("action", New SemanticResultValue("click", "mulligan")))
        mulliganChoices.Add("confirm")
        mulliganChoices.Add(myHand)
        mulliganBuilder.Append(mulliganChoices)

        Return mulliganBuilder
    End Function          ' Grammar for mulligan cards and confirm
    Public Function HeroPowerGrammar() As GrammarBuilder
        Dim heroChoice = New Choices(New SemanticResultValue("hero power", "hero"))
        'Attempt to read active hero power name

        Dim heroPowerEntity As Entity = Nothing

        heroPowerEntity = Entities.FirstOrDefault(Function(x)
                                                      Dim cardType = x.GetTag(GAME_TAG.CARDTYPE)
                                                      Dim cardController = x.GetTag(GAME_TAG.CONTROLLER)
                                                      Dim cardInPlay = x.IsInPlay

                                                      If cardType = Hearthstone.TAG_CARDTYPE.HERO_POWER And
                                                                cardController = friendlyID And
                                                                x.IsInPlay = True Then _
                                                                    Return True

                                                      Return False
                                                  End Function)


        If Not IsNothing(heroPowerEntity) Then
            Dim heroPowerName As String = heroPowerEntity.Card.Name
            heroChoice.Add(New SemanticResultValue(heroPowerName, "hero"))
            Return New GrammarBuilder(heroChoice)
        Else
            Return Nothing
        End If
    End Function         ' Grammar for name of hero power
    Public Function PlayCardGrammar() As GrammarBuilder
        Dim playChoices As New Choices


        'build grammar for card actions
        If FriendlyHand.DebugShowPhrases.Count Then
            'target card
            Dim targetCards As New GrammarBuilder
            targetCards.Append(New SemanticResultKey("action", "target"))
            targetCards.Append("card")
            targetCards.Append(myHand)
            playChoices.Add(targetCards)



            'play card to the left of friendly target
            If friendlyTargets.DebugShowPhrases.Count Then
                Dim playToFriendly As New GrammarBuilder
                If Not My.Settings.quickPlay Then _
                    playToFriendly.Append("play")
                playToFriendly.Append(myHand)
                playToFriendly.Append(New SemanticResultKey("action", New SemanticResultValue(New Choices("on", "to"), "play")))
                playToFriendly.Append(friendlyNames)
                playToFriendly.Append(friendlyTargets)

                playChoices.Add(playToFriendly)
            End If

            'play card to opposing target
            If opposingTargets.DebugShowPhrases.Count Then
                Dim playToOpposing As New GrammarBuilder
                If Not My.Settings.quickPlay Then _
                    playToOpposing.Append("play")
                playToOpposing.Append(myHand)
                playToOpposing.Append(New SemanticResultKey("action", New SemanticResultValue(New Choices("on", "to"), "play")))
                playToOpposing.Append(opposingNames)
                playToOpposing.Append(opposingTargets)

                playChoices.Add(playToOpposing)
            End If

            'play card with no target
            Dim playCards As New GrammarBuilder
            playCards.Append(New SemanticResultKey("action", "play"))
            playCards.Append(myHand)
            playChoices.Add(playCards)

        End If
        Return New GrammarBuilder(playChoices)
    End Function          ' Grammar for playing a card
    Public Function AttackTargetGrammar() As GrammarBuilder
        Dim attackChoices As New Choices

        ' attack <enemy> with <friendly>
        Dim attackFriendly As New GrammarBuilder
        attackFriendly.Append(New SemanticResultKey("action", "attack"))
        attackFriendly.Append(opposingTargets)
        attackFriendly.Append("with")
        attackFriendly.Append(friendlyTargets)
        attackChoices.Add(attackFriendly)

        ' <friendly> attack/go <enemy>
        Dim goTarget As New GrammarBuilder
        goTarget.Append(friendlyTargets)
        goTarget.Append(New SemanticResultKey("action", New Choices(New SemanticResultValue("go", "attack"), New SemanticResultValue("attack", "attack"))))
        goTarget.Append(opposingTargets)
        attackChoices.Add(goTarget)

        Return New GrammarBuilder(attackChoices)
    End Function      ' Grammar for attacking an enemy minion
    Public Function UseHeroPowerGrammar() As GrammarBuilder
        Dim heroTargetChoices As New Choices

        ' use <hero power>
        Dim heroPower As New GrammarBuilder
        If Not My.Settings.quickPlay Then _
            heroPower.Append("use")
        heroPower.Append(New SemanticResultKey("action", HeroPowerGrammar))
        heroTargetChoices.Add(heroPower)

        ' use <hero power> on friendly
        Dim heroFriendly As New GrammarBuilder
        If Not My.Settings.quickPlay Then _
            heroFriendly.Append("use")
        heroFriendly.Append(New SemanticResultKey("action", HeroPowerGrammar))
        heroFriendly.Append(New Choices("on", "to"))
        heroFriendly.Append(friendlyNames)
        heroFriendly.Append(friendlyTargets)
        heroTargetChoices.Add(heroFriendly)

        ' use <hero power> on opposing
        Dim heroOpposing As New GrammarBuilder
        If Not My.Settings.quickPlay Then _
            heroOpposing.Append("use")
        heroOpposing.Append(New SemanticResultKey("action", HeroPowerGrammar))
        heroOpposing.Append(New Choices("on", "to"))
        heroOpposing.Append(opposingNames)
        heroOpposing.Append(opposingTargets)
        heroTargetChoices.Add(heroOpposing)

        Return New GrammarBuilder(heroTargetChoices)
    End Function      ' Grammar for using hero power
    Public Function ClickTargetGrammar() As GrammarBuilder
        Dim clickChoices As New Choices

        ' click <friendly>
        Dim clickFriendly As New GrammarBuilder
        clickFriendly.Append(New SemanticResultKey("action", "click"))
        clickFriendly.Append(friendlyNames)
        clickFriendly.Append(friendlyTargets)
        clickChoices.Add(clickFriendly)

        ' click <opposing>
        Dim clickOpposing As New GrammarBuilder
        clickOpposing.Append(New SemanticResultKey("action", "click"))
        clickOpposing.Append(opposingNames)
        clickOpposing.Append(opposingTargets)
        clickChoices.Add(clickOpposing)

        Return New GrammarBuilder(clickChoices)
    End Function       ' Grammar for clicking target
    Public Function TargetTargetGrammar() As GrammarBuilder
        Dim targetChoices As New Choices

        Dim targetFriendly As New GrammarBuilder
        targetFriendly.Append(New SemanticResultKey("action", "target"))
        targetFriendly.Append(friendlyNames)
        targetFriendly.Append(friendlyTargets)
        targetChoices.Add(targetFriendly)

        Dim targetOpposing As New GrammarBuilder
        targetOpposing.Append(New SemanticResultKey("action", "target"))
        targetOpposing.Append(opposingNames)
        targetOpposing.Append(opposingTargets)
        targetChoices.Add(targetOpposing)

        Return New GrammarBuilder(targetChoices)
    End Function      ' Grammar for moving cursor to target
    Public Function ChooseOptionGrammar(Optional maxOptions As Integer = 4) As GrammarBuilder
        Dim chooseOption As New GrammarBuilder
        Dim optionChoices As New Choices
        For optMax = 1 To maxOptions
            optionChoices.Add(optMax.ToString)
        Next

        chooseOption.Append(New SemanticResultKey("action", "choose"))
        chooseOption.Append("option")
        chooseOption.Append(New SemanticResultKey("option", optionChoices))
        chooseOption.Append("of")
        chooseOption.Append(New SemanticResultKey("max", optionChoices))
        Return chooseOption
    End Function    ' Grammar for selecting a card option
    Public Function SayEmote() As GrammarBuilder
        Dim sayBuilder As New GrammarBuilder
        sayBuilder.Append(New SemanticResultKey("action", "say"))
        Dim sayChoices As New Choices
        sayChoices.Add(New SemanticResultValue("thanks", "thanks"))
        sayChoices.Add(New SemanticResultValue("thank you", "thanks"))
        sayChoices.Add(New SemanticResultValue("well played", "well played"))
        sayChoices.Add(New SemanticResultValue("greetings", "greetings"))
        sayChoices.Add(New SemanticResultValue("hello", "greetings"))
        sayChoices.Add(New SemanticResultValue("sorry", "sorry"))
        sayChoices.Add(New SemanticResultValue("oops", "oops"))
        sayChoices.Add(New SemanticResultValue("whoops", "oops"))
        sayChoices.Add(New SemanticResultValue("threaten", "threaten"))
        sayBuilder.Append(New SemanticResultKey("emote", sayChoices))
        Return sayBuilder
    End Function                 ' Grammar for saying emotes
    Public Function DebuggerGameCommands() As GrammarBuilder
        Dim debugChoices As New Choices
        debugChoices.Add("debug show cards")
        debugChoices.Add("debug show friendlies")
        debugChoices.Add("debug show enemies")
        Return New GrammarBuilder(debugChoices)
    End Function     ' Debugger only commands
    Public Sub InitializeGame()
        ' Initialize controller IDs
        friendlyID = Nothing
        opposingID = Nothing

        If Not IsNothing(PlayerEntity) And Not IsNothing(OpponentEntity) Then
            friendlyID = PlayerEntity.GetTag(GAME_TAG.CONTROLLER)
            opposingID = OpponentEntity.GetTag(GAME_TAG.CONTROLLER)
            RefreshGameData()
        End If
    End Sub
    Public Sub RefreshGameData()
        ' Build list of cards in hand
        If IsNothing(handCards) Then _
            handCards = New List(Of Entity)

        handCards.Clear()

        ' Recurse through list of game entities
        For Each e In Entities
            If e.IsInHand And e.GetTag(GAME_TAG.CONTROLLER) = friendlyID Then
                ' If entity is in player hand then add to list
                handCards.Add(e)
            End If
        Next

        ' Sort by position in hand
        handCards.Sort(Function(e1 As Entity, e2 As Entity)
                           Return e1.GetTag(GAME_TAG.ZONE_POSITION).CompareTo(e2.GetTag(GAME_TAG.ZONE_POSITION))
                       End Function)

        ' Build list of minions on board
        If IsNothing(boardFriendly) Then _
            boardFriendly = New List(Of Entity)

        If IsNothing(boardOpposing) Then _
            boardOpposing = New List(Of Entity)

        boardFriendly.Clear()
        boardOpposing.Clear()

        For Each e In Entities
            If e.IsInPlay And e.IsMinion Then
                If e.IsControlledBy(friendlyID) Then
                    boardFriendly.Add(e)
                ElseIf e.IsControlledBy(opposingID) Then
                    boardOpposing.Add(e)
                End If
            End If
        Next

        ' Sort by position on board
        boardFriendly.Sort(Function(e1 As Entity, e2 As Entity)
                               Return e1.GetTag(GAME_TAG.ZONE_POSITION).CompareTo(e2.GetTag(GAME_TAG.ZONE_POSITION))
                           End Function)

        boardOpposing.Sort(Function(e1 As Entity, e2 As Entity)
                               Return e1.GetTag(GAME_TAG.ZONE_POSITION).CompareTo(e2.GetTag(GAME_TAG.ZONE_POSITION))
                           End Function)

        friendlyTargets = FriendlyTargetGrammar()
        opposingTargets = OpposingTargetGrammar()
        myHand = FriendlyHand()
    End Sub ' Rebuilds arrays with data for cards in hand and on board

    Private ReadOnly Property Entities As Entity()
        Get
            ' Clone entities from game and return as array

            Return Helper.DeepClone(Game.Entities).Values.ToArray
        End Get
    End Property ' The list of entities for the current game
    Private ReadOnly Property PlayerEntity As Entity
        Get
            ' Return the Entity representing the player
            Return Entities.FirstOrDefault(Function(x) x.IsPlayer())
        End Get
    End Property ' The player's entity
    Private ReadOnly Property OpponentEntity As Entity
        Get
            ' Return the Entity representing the player
            Return Entities.FirstOrDefault(Function(x) x.IsOpponent())
        End Get
    End Property ' The opponent entity
End Class
