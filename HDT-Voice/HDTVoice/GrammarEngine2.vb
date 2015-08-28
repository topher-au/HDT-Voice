Imports System.Speech.Recognition
Imports Hearthstone_Deck_Tracker
Imports Hearthstone_Deck_Tracker.Hearthstone
Imports Hearthstone_Deck_Tracker.API
Imports Hearthstone_Deck_Tracker.Enums
Imports Hearthstone_Deck_Tracker.Hearthstone.Entities
Public Class GrammarEngine2

    Private friendlyID As Integer = 0
    Private opposingID As Integer = 0

    Private friendlyNames As Choices = CreateChoicesFromString(My.Resources.FRIENDLY)
    Private opposingNames As Choices = CreateChoicesFromString(My.Resources.OPPOSING)

    Private myHand As GrammarBuilder
    Private fTargets As GrammarBuilder
    Private oTargets As GrammarBuilder
    ''' <summary>
    ''' Ensures that the player and opponent IDs are up to date
    ''' </summary>
    Private Sub CheckIDs()
        If friendlyID = 0 Or opposingID = 0 Then
            StartNewGame()
        End If
    End Sub
    Private Function CreateChoicesFromString(CommaSeperated As String)
        Dim stringChoices As String() = CommaSeperated.Split(",")
        Dim finalChoices As New Choices
        For Each choices In stringChoices
            finalChoices.Add(choices)
        Next
        Return finalChoices
    End Function
    Public Function CreateChoicesFromEntities(EntityList As List(Of Entity)) As Choices
        CheckIDs()

        ' Get a list of current cards
        Dim entityChoices As New Choices

        If EntityList.Count > 0 Then
            Do
                For Each entity In EntityList
                    Dim cardName = entity.Card.Name
                    Dim cardPos = EntityList.IndexOf(entity) + 1
                    Dim cardSame = EntityList.FindAll(Function(x) x.CardId = entity.CardId)
                    Dim cardSemanticValue As String = SemanticEntityValue(GrammarEntityType.Entity, entity.Id)

                    entityChoices.Add(New GrammarBuilder(New SemanticResultValue(
                                                       cardName,
                                                       SemanticEntityValue(GrammarEntityType.Entity, entity.Id))))

                    If cardSame.Count > 1 Then
                        For Each card In cardSame
                            entityChoices.Add(New GrammarBuilder(New SemanticResultValue(
                                                               String.Format("{0} {1}", cardName, cardSame.IndexOf(card) + 1),
                                                               SemanticEntityValue(GrammarEntityType.Entity, card.Id))))
                            EntityList.Remove(card)
                        Next
                        Continue Do
                    Else
                        EntityList.Remove(entity)
                        Continue Do
                    End If
                Next
                Exit Do
            Loop
            Return entityChoices
        End If
        Return Nothing
    End Function 'Returns a list of card names from the current game

    Public ReadOnly Property PlayCardGramma() As GrammarBuilder
        Get
            Dim cardHand = GetCardsInHand()

            Dim playCardChoices As New Choices
            Dim cardNames As Choices = CreateChoicesFromEntities(cardHand)

            ' Generate card names and numbers
            Dim playCardByName As New GrammarBuilder
            If My.Settings.boolQuickPlay Then _
                playCardByName.Append(My.Resources.PLAYCARD)
            playCardByName.Append(New SemanticResultKey("play", cardNames))
            playCardChoices.Add(playCardByName)

            Dim playCardByNumber As New GrammarBuilder
            For Each c In cardHand

                Dim cardNumber = cardHand.IndexOf(c) + 1

                If My.Settings.boolQuickPlay Then
                    playCardByNumber.Append(My.Resources.PLAYCARD)
                End If
                playCardByNumber.Append(My.Resources.CARD)
                playCardByNumber.Append(New SemanticResultKey("play", New SemanticResultValue(
                                                              cardNumber.ToString,
                                                              SemanticEntityValue(GrammarEntityType.Card, cardNumber))))
                playCardChoices.Add(playCardByNumber)
            Next


            Dim finalChoices As New Choices

            'Play card with no target
            finalChoices.Add(playCardChoices)

            'Play card with friendly target
            Dim playToFriendly As New GrammarBuilder
            playToFriendly.Append(playCardChoices)


            'Play card with opposing target

            Return playCardChoices
        End Get
    End Property
    Private ReadOnly Property FriendlyTargets() As Choices
        Get
            CheckIDs()
            Dim friendlyChoices As New Choices
            Dim friendlies = GetFriendlyMinions()
            friendlyChoices.Add(CreateChoicesFromEntities(friendlies))

            For Each e In friendlies
                Dim friendlyNum As New GrammarBuilder
                friendlyNum.Append(My.Resources.MINION)
                friendlyNum.Append((friendlies.IndexOf(e) + 1).ToString)
                friendlyChoices.Add(friendlyNum)
            Next

            Return friendlyChoices
        End Get
    End Property
    Private ReadOnly Property OpposingTargets() As Choices
        Get
            CheckIDs()
            Dim opposingChoices As New Choices
            Dim opposingMinions = GetOpposingMinions()
            opposingChoices.Add(CreateChoicesFromEntities(opposingMinions))

            For Each e In opposingMinions
                Dim opposingNum As New GrammarBuilder
                opposingNum.Append(My.Resources.MINION)
                opposingNum.Append((opposingMinions.IndexOf(e) + 1).ToString)
                opposingChoices.Add(opposingNum)
            Next

            Return opposingChoices
        End Get
    End Property

    Private Function FriendlyHandGrammar() As GrammarBuilder
        ' Check if there are any cards in hand
        Dim handCards = GetCardsInHand()

        If handCards.Count > 0 Then
            Dim cardGrammar As New GrammarBuilder
            Dim handGrammarNames, handGrammarNumbers, handGrammarCardNumbers As New Choices
            ' Build

            For Each e In handCards
                Dim CardName As New String(e.Card.Name)
                Dim CardPos As Integer = e.GetTag(GAME_TAG.ZONE_POSITION)
                Dim CardInstances = handCards.FindAll(Function(x) x.CardId = e.CardId)

                If CardInstances.Count > 1 Then ' if we have multiple cards with the same name, add a numeric identifier
                    If Not handGrammarNames.ToGrammarBuilder.DebugShowPhrases.Contains(CardName) Then
                        handGrammarNames.Add(New SemanticResultValue(CardName, SemanticEntityValue(GrammarEntityType.Entity, e.Id)))
                        handGrammarNumbers.Add(New SemanticResultValue(CardPos.ToString.Trim, SemanticEntityValue(GrammarEntityType.Card, CardPos)))
                        handGrammarCardNumbers.Add(New SemanticResultValue("card " & CardPos.ToString.Trim, SemanticEntityValue(GrammarEntityType.Card, CardPos)))
                    Else
                        Dim CardNum As Integer = CardInstances.IndexOf(CardInstances.Find(Function(x) x.Id = e.Id)) + 1
                        CardName &= " " & CardNum.ToString
                        handGrammarNames.Add(New SemanticResultValue(CardName, SemanticEntityValue(GrammarEntityType.Entity, e.Id)))
                        handGrammarNumbers.Add(New SemanticResultValue(CardPos.ToString.Trim, SemanticEntityValue(GrammarEntityType.Card, CardPos)))
                        handGrammarCardNumbers.Add(New SemanticResultValue("card " & CardPos.ToString.Trim, SemanticEntityValue(GrammarEntityType.Card, CardPos)))
                    End If
                Else
                    handGrammarNames.Add(New SemanticResultValue(CardName, SemanticEntityValue(GrammarEntityType.Entity, e.Id)))
                    handGrammarNumbers.Add(New SemanticResultValue(CardPos.ToString.Trim, SemanticEntityValue(GrammarEntityType.Card, CardPos)))
                    handGrammarCardNumbers.Add(New SemanticResultValue("card " & CardPos.ToString.Trim, SemanticEntityValue(GrammarEntityType.Card, CardPos)))
                End If

            Next
            cardGrammar.Append(New SemanticResultKey("card", New Choices(handGrammarNames, handGrammarNumbers, handGrammarCardNumbers)))
            Return cardGrammar
        Else
            Return New GrammarBuilder("null")
        End If
    End Function      ' Retrieves current hand and generates Grammar
    Private Function FriendlyTargetGrammar() As GrammarBuilder
        ' Build the grammar for friendly minions and hero
        Dim friendlyBuilder As New GrammarBuilder
        Dim friendlyChoices As New Choices

        Dim boardFriendly = GetFriendlyMinions()

        ' If there are friendly minions on the board
        If boardFriendly.Count > 0 Then
            Dim friendlyGrammarNames, friendlyGrammarNumbers As New Choices

            'Recurse through all friendly minions
            For Each minion In boardFriendly

                Dim MinionName As New String(minion.Card.Name) ' The minion's name
                Dim MinionInst = boardFriendly.FindAll(Function(x) x.CardId = minion.CardId) ' A list of all friendly minions the same
                Dim MinionPos As Integer = minion.GetTag(GAME_TAG.ZONE_POSITION)

                ' If multiple of the same minion
                If MinionInst.Count > 1 Then

                    ' Check if the minion has already been added to the Grammar
                    If Not friendlyGrammarNames.ToGrammarBuilder.DebugShowPhrases.Contains(MinionName) Then

                        ' If not, add an un-numbered  and #1 minion
                        friendlyGrammarNames.Add(New SemanticResultValue(MinionName, SemanticEntityValue(GrammarEntityType.Entity, minion.Id)))
                        friendlyGrammarNames.Add(New SemanticResultValue(MinionName & " 1", SemanticEntityValue(GrammarEntityType.Entity, minion.Id)))

                        ' and the minion by number on the board
                        friendlyGrammarNumbers.Add(New SemanticResultValue(String.Format("minion {0}", MinionPos), SemanticEntityValue(GrammarEntityType.Friendly, MinionPos)))

                    Else
                        Dim MinionNum As Integer = MinionInst.IndexOf(minion) + 1
                        MinionName = String.Format("{0} {1}", MinionName, MinionNum)
                        friendlyGrammarNames.Add(New SemanticResultValue(MinionName, SemanticEntityValue(GrammarEntityType.Entity, minion.Id)))
                        friendlyGrammarNumbers.Add(New SemanticResultValue(String.Format("minion {0}", MinionPos), SemanticEntityValue(GrammarEntityType.Friendly, MinionPos)))
                    End If

                Else
                    friendlyGrammarNames.Add(New SemanticResultValue(MinionName, SemanticEntityValue(GrammarEntityType.Entity, minion.Id)))
                    friendlyGrammarNumbers.Add(New SemanticResultValue(String.Format("minion {0}", MinionPos), SemanticEntityValue(GrammarEntityType.Friendly, MinionPos)))
                End If


            Next

            friendlyChoices.Add(friendlyGrammarNames, friendlyGrammarNumbers)
        End If

        If Not IsNothing(PlayerEntity) Then
            Dim friendlyHero As New Choices
            friendlyHero.Add(New SemanticResultValue("hero", "E" & PlayerEntity.Id))
            friendlyHero.Add(New SemanticResultValue("face", "E" & PlayerEntity.Id))
            friendlyChoices.Add(friendlyHero)
        End If

        friendlyBuilder.Append(New SemanticResultKey("friendly", friendlyChoices))
        Return friendlyBuilder
    End Function    ' Generates Grammar for all friendly targets
    Private Function OpposingTargetGrammar() As GrammarBuilder
        ' Build the grammar for friendly minions and hero
        Dim opposingBuilder As New GrammarBuilder
        Dim opposingChoices As New Choices

        Dim boardOpposing = GetOpposingMinions()

        ' If there are friendly minions on the board
        If boardOpposing.Count > 0 Then
            Dim opposingGrammarNames, opposingGrammarNumbers As New Choices

            'Recurse through all opposing minions
            For Each minion In boardOpposing

                Dim MinionName As New String(minion.Card.Name) ' The minion's name
                Dim MinionInst = boardOpposing.FindAll(Function(x) x.CardId = minion.CardId) ' A list of all opposing minions the same
                Dim MinionPos As Integer = minion.GetTag(GAME_TAG.ZONE_POSITION)

                ' If multiple of the same minion
                If MinionInst.Count > 1 Then

                    ' Check if the minion has already been added to the Grammar
                    If Not opposingGrammarNames.ToGrammarBuilder.DebugShowPhrases.Contains(MinionName) Then

                        ' If not, add an un-numbered  and #1 minion
                        opposingGrammarNames.Add(New SemanticResultValue(MinionName, SemanticEntityValue(GrammarEntityType.Entity, minion.Id)))
                        opposingGrammarNames.Add(New SemanticResultValue(MinionName & " 1", SemanticEntityValue(GrammarEntityType.Entity, minion.Id)))

                        ' and the minion by number on the board
                        opposingGrammarNumbers.Add(New SemanticResultValue(String.Format("minion {0}", MinionPos), SemanticEntityValue(GrammarEntityType.Opposing, MinionPos)))

                    Else
                        Dim MinionNum As Integer = MinionInst.IndexOf(minion) + 1
                        MinionName = String.Format("{0} {1}", MinionName, MinionNum)
                        opposingGrammarNames.Add(New SemanticResultValue(MinionName, SemanticEntityValue(GrammarEntityType.Entity, minion.Id)))
                        opposingGrammarNumbers.Add(New SemanticResultValue(String.Format("minion {0}", MinionPos), SemanticEntityValue(GrammarEntityType.Opposing, MinionPos)))
                    End If

                Else
                    opposingGrammarNames.Add(New SemanticResultValue(MinionName, SemanticEntityValue(GrammarEntityType.Entity, minion.Id)))
                    opposingGrammarNumbers.Add(New SemanticResultValue(String.Format("minion {0}", MinionPos), SemanticEntityValue(GrammarEntityType.Opposing, MinionPos)))
                End If


            Next

            opposingChoices.Add(opposingGrammarNames, opposingGrammarNumbers)
        End If

        If Not IsNothing(OpponentEntity) Then
            Dim opposingHero As New Choices
            opposingHero.Add(New SemanticResultValue("hero", "E" & OpponentEntity.Id))
            opposingHero.Add(New SemanticResultValue("face", "E" & OpponentEntity.Id))
            opposingChoices.Add(opposingHero)
        End If

        opposingBuilder.Append(New SemanticResultKey("opposing", opposingChoices))
        Return opposingBuilder

    End Function    ' Generates Grammar for all opposing targets
    Private Function HeroPowerNameGrammar() As GrammarBuilder
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
            Return New GrammarBuilder(heroChoice)
        End If
    End Function     ' Retrieves the active hero power and generates Grammar
    Public Sub New()
        GameEvents.OnGameStart.Add(New Action(AddressOf StartNewGame))
        StartNewGame()
    End Sub
    Public ReadOnly Property MenuGrammar As Grammar
        Get
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
            menuChoices.Add(New SemanticResultKey("menu", "click"))
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

            Return New Grammar(menuChoices)
        End Get
    End Property
    Public ReadOnly Property MulliganGrammar As Grammar
        Get
            myHand = FriendlyHandGrammar()

            Dim mulliganBuilder As New GrammarBuilder
            Dim mulliganChoices As New Choices

            mulliganBuilder.Append(New SemanticResultKey("action", New SemanticResultValue("click", "mulligan")))
            mulliganChoices.Add("confirm")
            mulliganChoices.Add(myHand)
            mulliganBuilder.Append(mulliganChoices)

            Return New Grammar(mulliganBuilder)
        End Get
    End Property           ' Builds the Grammar used during mulligan
    Public ReadOnly Property GameGrammar As Grammar
        Get
            If friendlyID = 0 Then
                StartNewGame()
            End If
            myHand = FriendlyHandGrammar()
            fTargets = FriendlyTargetGrammar()
            oTargets = OpposingTargetGrammar()

            Dim finalChoice As New Choices

            finalChoice.Add(UseHeroPowerGrammar)
            finalChoice.Add(PlayCardGrammar)
            finalChoice.Add(AttackTargetGrammar)
            finalChoice.Add(ClickTargetGrammar)
            finalChoice.Add(TargetTargetGrammar)
            finalChoice.Add(ChooseOptionGrammar)
            finalChoice.Add(SayEmote)

            Dim endTurn As New GrammarBuilder
            endTurn.Append(New SemanticResultKey("action", "end"))
            endTurn.Append("turn")
            finalChoice.Add(endTurn)
            finalChoice.Add(New SemanticResultKey("action", "click"))
            finalChoice.Add(New SemanticResultKey("action", "cancel"))

            If Debugger.IsAttached Then
                finalChoice.Add(DebuggerGameCommands)
            End If

            Debug.WriteLine(finalChoice.ToGrammarBuilder.DebugShowPhrases)

            Return New Grammar(finalChoice)
        End Get
    End Property ' Runs all of the Grammar functions below and returns a single GrammarBuilder

    Private Function PlayCardGrammar() As GrammarBuilder
        Dim playChoices As New Choices

        'build grammar for card actions
        If FriendlyHandGrammar.DebugShowPhrases.Count Then

            'play card to the left of friendly target
            If fTargets.DebugShowPhrases.Count Then
                Dim playToFriendly As New GrammarBuilder
                If Not My.Settings.boolQuickPlay Then _
                    playToFriendly.Append("play")
                playToFriendly.Append(myHand)
                playToFriendly.Append(New SemanticResultKey("action", New SemanticResultValue(New Choices("on", "to"), "play")))
                playToFriendly.Append(friendlyNames)
                playToFriendly.Append(fTargets)

                playChoices.Add(playToFriendly)
            End If

            'play card to opposing target
            If oTargets.DebugShowPhrases.Count Then
                Dim playToOpposing As New GrammarBuilder
                If Not My.Settings.boolQuickPlay Then _
                    playToOpposing.Append("play")
                playToOpposing.Append(myHand)
                playToOpposing.Append(New SemanticResultKey("action", New SemanticResultValue(New Choices("on", "to"), "play")))
                playToOpposing.Append(opposingNames)
                playToOpposing.Append(oTargets)

                playChoices.Add(playToOpposing)
            End If

            'play card with no target
            Dim playCards As New GrammarBuilder
            playCards.Append(New SemanticResultKey("action", "play"))
            playCards.Append(myHand)
            playChoices.Add(playCards)

        End If
        Return New GrammarBuilder(playChoices)
    End Function          ' Generates Grammar for playing a card
    Private Function AttackTargetGrammar() As GrammarBuilder
        Dim attackChoices As New Choices

        ' attack <enemy> with <friendly>
        Dim attackFriendly As New GrammarBuilder
        attackFriendly.Append(New SemanticResultKey("action", "attack"))
        attackFriendly.Append(oTargets)
        attackFriendly.Append("with")
        attackFriendly.Append(fTargets)
        attackChoices.Add(attackFriendly)

        ' <friendly> attack/go <enemy>
        Dim goTarget As New GrammarBuilder
        goTarget.Append(fTargets)
        goTarget.Append(New SemanticResultKey("action", New Choices(New SemanticResultValue("go", "attack"), New SemanticResultValue("attack", "attack"))))
        goTarget.Append(oTargets)
        attackChoices.Add(goTarget)

        Return New GrammarBuilder(attackChoices)
    End Function      ' Generates Grammar for attacking an enemy minion
    Private Function UseHeroPowerGrammar() As GrammarBuilder
        Dim heroTargetChoices As New Choices

        ' use <hero power>
        Dim heroPower As New GrammarBuilder
        If Not My.Settings.boolQuickPlay Then _
            heroPower.Append("use")
        heroPower.Append(New SemanticResultKey("action", HeroPowerNameGrammar))
        heroTargetChoices.Add(heroPower)

        ' use <hero power> on friendly
        Dim heroFriendly As New GrammarBuilder
        If Not My.Settings.boolQuickPlay Then _
            heroFriendly.Append("use")
        heroFriendly.Append(New SemanticResultKey("action", HeroPowerNameGrammar))
        heroFriendly.Append(friendlyNames)
        heroFriendly.Append(fTargets)
        heroTargetChoices.Add(heroFriendly)

        ' use <hero power> on opposing
        Dim heroOpposing As New GrammarBuilder
        If Not My.Settings.boolQuickPlay Then _
            heroOpposing.Append("use")
        heroOpposing.Append(New SemanticResultKey("action", HeroPowerNameGrammar))
        heroOpposing.Append(opposingNames)
        heroOpposing.Append(oTargets)
        heroTargetChoices.Add(heroOpposing)

        Return New GrammarBuilder(heroTargetChoices)
    End Function      ' Generates Grammar for using hero power
    Private Function ClickTargetGrammar() As GrammarBuilder
        Dim clickChoices As New Choices

        ' click <friendly>
        Dim clickFriendly As New GrammarBuilder
        clickFriendly.Append(New SemanticResultKey("action", "click"))
        clickFriendly.Append(friendlyNames)
        clickFriendly.Append(fTargets)
        clickChoices.Add(clickFriendly)

        ' click <opposing>
        Dim clickOpposing As New GrammarBuilder
        clickOpposing.Append(New SemanticResultKey("action", "click"))
        clickOpposing.Append(opposingNames)
        clickOpposing.Append(oTargets)
        clickChoices.Add(clickOpposing)

        Return New GrammarBuilder(clickChoices)
    End Function       ' Generates Grammar for clicking target
    Private Function TargetTargetGrammar() As GrammarBuilder
        Dim targetChoices As New Choices

        Dim targetCard As New GrammarBuilder
        targetCard.Append(New SemanticResultKey("action", "target"))
        targetCard.Append(myHand)
        targetChoices.Add(targetCard)

        Dim targetFriendly As New GrammarBuilder
        targetFriendly.Append(New SemanticResultKey("action", "target"))
        targetFriendly.Append(friendlyNames)
        targetFriendly.Append(fTargets)
        targetChoices.Add(targetFriendly)

        Dim targetOpposing As New GrammarBuilder
        targetOpposing.Append(New SemanticResultKey("action", "target"))
        targetOpposing.Append(opposingNames)
        targetOpposing.Append(oTargets)
        targetChoices.Add(targetOpposing)

        Return New GrammarBuilder(targetChoices)
    End Function      ' Generates Grammar for moving cursor to target
    Private Function ChooseOptionGrammar() As GrammarBuilder

        Dim chooseOption As New GrammarBuilder
        Dim optionChoices As New Choices
        For optMax = 1 To 4
            optionChoices.Add(optMax.ToString)
        Next

        chooseOption.Append(New SemanticResultKey("action", "choose"))
        chooseOption.Append("option")
        chooseOption.Append(New SemanticResultKey("option", optionChoices))
        chooseOption.Append("of")
        chooseOption.Append(New SemanticResultKey("max", optionChoices))
        Return chooseOption
    End Function      ' Generates Grammar for selecting a card option
    Private Function SayEmote() As GrammarBuilder
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
    End Function                 ' Generates Grammar for saying emotes
    Private Function DebuggerGameCommands() As GrammarBuilder
        Dim debugChoices As New Choices
        debugChoices.Add("debug show cards")
        debugChoices.Add("debug show friendlies")
        debugChoices.Add("debug show enemies")
        Return New GrammarBuilder(debugChoices)
    End Function     ' Debugger only commands

    Private Sub StartNewGame()
        friendlyID = Nothing
        opposingID = Nothing

        If Not IsNothing(PlayerEntity) And Not IsNothing(OpponentEntity) Then
            friendlyID = PlayerEntity.GetTag(GAME_TAG.CONTROLLER)
            opposingID = OpponentEntity.GetTag(GAME_TAG.CONTROLLER)
        End If
    End Sub                                     ' Re-initalizes controller IDs

    Private Function GetCardsInHand() As List(Of Entity)
        Dim CardsInHand As New List(Of Entity)

        For Each e In Entities
            If e.IsInHand And e.GetTag(GAME_TAG.CONTROLLER) = friendlyID Then
                ' If entity is in player hand then add to list
                CardsInHand.Add(e)
            End If
        Next

        CardsInHand.Sort(Function(e1 As Entity, e2 As Entity)
                             Return e1.GetTag(GAME_TAG.ZONE_POSITION).CompareTo(e2.GetTag(GAME_TAG.ZONE_POSITION))
                         End Function)

        Return CardsInHand
    End Function           ' Returns an ordered list of the current cards in hand
    Private Function GetFriendlyMinions() As List(Of Entity)
        Dim FriendlyMinions As New List(Of Entity)

        For Each e In Entities
            If e.IsInPlay And e.IsMinion And e.IsControlledBy(friendlyID) Then
                FriendlyMinions.Add(e)
            End If
        Next

        FriendlyMinions.Sort(Function(e1 As Entity, e2 As Entity)
                                 Return e1.GetTag(GAME_TAG.ZONE_POSITION).CompareTo(e2.GetTag(GAME_TAG.ZONE_POSITION))
                             End Function)

        Return FriendlyMinions
    End Function       ' Returns an ordered list of the current friendly minions
    Private Function GetOpposingMinions() As List(Of Entity)
        Dim OpposingMinions As New List(Of Entity)

        For Each e In Entities
            If e.IsInPlay And e.IsMinion And e.IsControlledBy(opposingID) Then
                OpposingMinions.Add(e)
            End If
        Next

        OpposingMinions.Sort(Function(e1 As Entity, e2 As Entity)
                                 Return e1.GetTag(GAME_TAG.ZONE_POSITION).CompareTo(e2.GetTag(GAME_TAG.ZONE_POSITION))
                             End Function)

        Return OpposingMinions
    End Function       ' Returns an ordered list of the current opposing minions

    Public Function GetEntityFromSemantic(SemanticValue As String) As Entity
        ' The Semantic Value is HDT-Voice's shorthand way of naming entities, this is used
        ' to allow numbered minion and card usage without attaching the numbers to a specific
        ' entity, hopefully resolving some issues with the game selecting the wrong entity.

        ' Typically in format: C1 = Card1, F7 = Friendly minion 7, E18 = entity 18, etc.


        Dim finalEntity As Entity

        'Check if just an entity ID was passed and return if so
        If IsNumeric(SemanticValue) Then
            finalEntity = Entities(SemanticValue - 1)
            Return finalEntity
        End If

        ' Get the semantic ID used to identify the entity
        Dim SemanticID As Integer = Convert.ToInt32(SemanticValue.Substring(1))


        Select Case SemanticValue.Substring(0, 1)
            Case "F" 'Friendly minions
                Dim Friendlies = GetFriendlyMinions()
                finalEntity = Friendlies.Item(SemanticID - 1)
            Case "O" 'Opposing minions
                Dim Enemies = GetOpposingMinions()
                finalEntity = Enemies.Item(SemanticID - 1)
            Case "C" 'Cards in hand
                Dim Cards = GetCardsInHand()
                finalEntity = Cards.Item(SemanticID - 1)
            Case "E" 'Specific entity
                finalEntity = Entities(SemanticID - 1)
            Case Else
                finalEntity = Nothing
        End Select

        Return finalEntity
    End Function ' Resolves a semantic value provided by the grammar engine to an entity

    Private ReadOnly Property Entities As Entity()
        Get
            ' Clone entities from game and return as array
            Dim EntArray = Helper.DeepClone(Core.Game.Entities).Values.ToArray
            Return EntArray
        End Get
    End Property                ' Clones Entites from HDT and creates an array
    Private ReadOnly Property PlayerEntity As Entity
        Get
            ' Return the Entity representing the player
            Try
                Return Entities.First(Function(x) x.IsPlayer())
            Catch ex As Exception
                Return Nothing
            End Try
        End Get
    End Property              ' Gets the player's current Entity
    Private ReadOnly Property OpponentEntity As Entity
        Get
            ' Return the Entity representing the player
            Try
                Return Entities.First(Function(x) x.IsOpponent())
            Catch ex As Exception
                Return Nothing
            End Try

        End Get
    End Property            ' Gets the opponent's current Entity
    Private Function SemanticEntityValue(Type As GrammarEntityType, Value As Integer) As String
        Dim semanticType As New String("")
        Select Case Type
            Case GrammarEntityType.Entity
                semanticType = "E"
            Case GrammarEntityType.Card
                semanticType = "C"
            Case GrammarEntityType.Friendly
                semanticType = "F"
            Case GrammarEntityType.Opposing
                semanticType = "O"
        End Select
        Return String.Format("{0}{1}", semanticType, Value)
    End Function
    Enum GrammarEntityType
        Entity
        Card
        Friendly
        Opposing
    End Enum
End Class
