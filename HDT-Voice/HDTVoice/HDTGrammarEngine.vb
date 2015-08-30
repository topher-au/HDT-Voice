Imports System.Speech.Recognition
Imports Hearthstone_Deck_Tracker
Imports Hearthstone_Deck_Tracker.Hearthstone
Imports Hearthstone_Deck_Tracker.API
Imports Hearthstone_Deck_Tracker.Enums
Imports Hearthstone_Deck_Tracker.Hearthstone.Entities
Public Class HDTGrammarEngine

    Private friendlyID As Integer = 0
    Private opposingID As Integer = 0

    Private friendlyNames As Choices = ResChoice("FRIENDLY")
    Private opposingNames As Choices = ResChoice("OPPOSING")
    Private heroNames As Choices = ResChoice("HERO")

    Public Function MulliganGrammar() As Grammar
        CheckGameState()

        Dim cardsInHand = GetCardsInHand()
        Dim mulliganChoices As New Choices

        Dim mulliBuilder As New GrammarBuilder
        If Not My.Settings.boolQuickPlay Then _
                mulliBuilder.Append(ResChoice("MULLIGANCLICK"))

        If cardsInHand.Count > 0 Then
            Dim myHand = FriendlyHandChoices()
            mulliganChoices.Add(New SemanticResultKey("mulligan", myHand))
        End If
        mulliganChoices.Add(ResChoice("MULLIGANCONFIRM"))
        mulliBuilder.Append(New SemanticResultKey("mulligan", mulliganChoices))

        Dim returnGrammar As New Grammar(mulliBuilder)
        returnGrammar.Name = Reflection.MethodBase.GetCurrentMethod.Name.ToString
        Return returnGrammar
    End Function

    Public Function PlayCardGrammar() As Grammar
        CheckGameState()
        Dim finalChoices As New Choices
        Dim playCard As New GrammarBuilder

        If Not My.Settings.boolQuickPlay Then _
            playCard.Append(ResChoice("PLAYCARD"))
        playCard.Append(New SemanticResultKey("play", FriendlyHandChoices()))
        finalChoices.Add(playCard)

        'Play card with friendly target
        Dim playToFriendly As New GrammarBuilder
        If Not My.Settings.boolQuickPlay Then _
            playToFriendly.Append(ResChoice("PLAYCARD"))
        playToFriendly.Append(New SemanticResultKey("play", FriendlyHandChoices()))
        playToFriendly.Append(ResChoice("PLAYTO"))
        playToFriendly.Append(friendlyNames)
        playToFriendly.Append(New SemanticResultKey("target", FriendlyTargetChoices))
        finalChoices.Add(playToFriendly)

        'Play card with opposing target
        Dim playToOpposingTarget As New GrammarBuilder
        If Not My.Settings.boolQuickPlay Then _
            playToOpposingTarget.Append(ResChoice("PLAYCARD"))
        playToOpposingTarget.Append(New SemanticResultKey("play", FriendlyHandChoices()))
        playToOpposingTarget.Append(ResChoice("PLAYTO"))
        playToOpposingTarget.Append(opposingNames)
        playToOpposingTarget.Append(New SemanticResultKey("target", OpposingTargetChoices))
        finalChoices.Add(playToOpposingTarget)

        Dim returnGrammar As New Grammar(finalChoices)
        returnGrammar.Name = Reflection.MethodBase.GetCurrentMethod.Name.ToString
        Return returnGrammar
    End Function
    Public Function AttackTargetGrammar() As Grammar
        CheckGameState()
        Dim attackChoices As New Choices

        If My.Settings.boolQuickPlay Then
            Dim goTarget As New GrammarBuilder
            goTarget.Append(New SemanticResultKey("attack", FriendlyTargetChoices))
            goTarget.Append(ResChoice("ATTACKSHORT"))
            goTarget.Append(New SemanticResultKey("target", OpposingTargetChoices))
            attackChoices.Add(goTarget)
        Else
            Dim attackTarget As New GrammarBuilder
            attackTarget.Append(ResChoice("ATTACKLONG"))
            attackTarget.Append(New SemanticResultKey("attack", OpposingTargetChoices))
            attackTarget.Append(ResChoice("ATTACKWITH"))
            attackTarget.Append(New SemanticResultKey("target", FriendlyTargetChoices))
            attackChoices.Add(attackTarget)
        End If

        Dim returnGrammar As New Grammar(attackChoices)
        returnGrammar.Name = Reflection.MethodBase.GetCurrentMethod.Name.ToString
        Return returnGrammar
    End Function
    Public Function UseHeroPowerGrammar() As Grammar
        CheckGameState()

        Dim heroPowerNames As Choices = GetHeroPowerNames()
        Dim heroChoices As New Choices

        Dim useHeroPower As New GrammarBuilder
        If Not My.Settings.boolQuickPlay Then _
            useHeroPower.Append(ResChoice("USEHEROPOWER"))
        useHeroPower.Append(New SemanticResultKey("hero", heroPowerNames))
        heroChoices.Add(useHeroPower)

        Dim useHeroPowerFriendly As New GrammarBuilder
        If Not My.Settings.boolQuickPlay Then _
            useHeroPowerFriendly.Append(ResChoice("USEHEROPOWER"))
        useHeroPowerFriendly.Append(New SemanticResultKey("hero", heroPowerNames))
        useHeroPowerFriendly.Append(ResChoice("PLAYON"))
        useHeroPowerFriendly.Append(friendlyNames)
        useHeroPowerFriendly.Append(New SemanticResultKey("target", FriendlyTargetChoices))
        heroChoices.Add(useHeroPowerFriendly)

        Dim useHeroPowerOpposing As New GrammarBuilder
        If Not My.Settings.boolQuickPlay Then _
            useHeroPowerOpposing.Append(ResChoice("USEHEROPOWER"))
        useHeroPowerOpposing.Append(New SemanticResultKey("hero", heroPowerNames))
        useHeroPowerOpposing.Append(ResChoice("PLAYON"))
        useHeroPowerOpposing.Append(opposingNames)
        useHeroPowerOpposing.Append(New SemanticResultKey("target", OpposingTargetChoices))
        heroChoices.Add(useHeroPowerOpposing)

        Dim returnGrammar As New Grammar(heroChoices)
        returnGrammar.Name = Reflection.MethodBase.GetCurrentMethod.Name.ToString
        Return returnGrammar
    End Function
    Public Function ClickGrammar() As Grammar
        CheckGameState()

        Dim clickChoices As New Choices

        Dim clickCard As New GrammarBuilder
        clickCard.Append(ResChoice("CLICK"))
        clickCard.Append(ResChoice("CARD"))
        clickCard.Append(New SemanticResultKey("click", FriendlyHandChoices(False)))
        clickChoices.Add(clickCard)

        Dim clickFriendly As New GrammarBuilder
        clickFriendly.Append(ResChoice("CLICK"))
        clickFriendly.Append(ResChoice("FRIENDLY"))
        clickFriendly.Append(New SemanticResultKey("click", FriendlyTargetChoices))
        clickChoices.Add(clickFriendly)

        Dim clickOpposing As New GrammarBuilder
        clickOpposing.Append(ResChoice("CLICK"))
        clickOpposing.Append(ResChoice("OPPOSING"))
        clickOpposing.Append(New SemanticResultKey("click", OpposingTargetChoices))
        clickChoices.Add(clickOpposing)

        Dim clickLeft As New GrammarBuilder
        clickLeft.Append(New SemanticResultKey("click", New SemanticResultValue(ResChoice("CLICK"), "left")))
        clickChoices.Add(clickLeft)

        Dim clickRight As New GrammarBuilder
        clickRight.Append(New SemanticResultKey("click", New SemanticResultValue(ResChoice("CANCELCLICK"), "right")))
        clickChoices.Add(clickRight)

        Dim returnGrammar As New Grammar(clickChoices)
        returnGrammar.Name = Reflection.MethodBase.GetCurrentMethod.Name.ToString
        Return returnGrammar
    End Function
    Public Function TargetGrammar() As Grammar
        CheckGameState()

        Dim targetChoices As New Choices

        Dim targetCard As New GrammarBuilder
        targetCard.Append(ResChoice("TARGET"))
        targetCard.Append(ResChoice("CARD"))
        targetCard.Append(New SemanticResultKey("target", FriendlyHandChoices(False)))
        targetChoices.Add(targetCard)

        Dim targetFriendly As New GrammarBuilder
        targetFriendly.Append(ResChoice("TARGET"))
        targetFriendly.Append(ResChoice("FRIENDLY"))
        targetFriendly.Append(New SemanticResultKey("target", FriendlyTargetChoices))
        targetChoices.Add(targetFriendly)

        Dim targetOpposing As New GrammarBuilder
        targetOpposing.Append(ResChoice("TARGET"))
        targetOpposing.Append(ResChoice("OPPOSING"))
        targetOpposing.Append(New SemanticResultKey("target", OpposingTargetChoices))
        targetChoices.Add(targetOpposing)


        Dim returnGrammar As New Grammar(targetChoices)
        returnGrammar.Name = Reflection.MethodBase.GetCurrentMethod.Name.ToString
        Return returnGrammar
    End Function
    Public Function EndTurnGrammar() As Grammar
        CheckGameState()
        Dim endTurnChoices As New Choices
        If My.Settings.boolQuickPlay Then
            endTurnChoices.Add(New SemanticResultKey("end", ResChoice("ENDTURNSHORT")))
        Else
            endTurnChoices.Add(New SemanticResultKey("end", ResChoice("ENDTURNLONG")))
        End If

        Dim returnGrammar As New Grammar(endTurnChoices)
        returnGrammar.Name = Reflection.MethodBase.GetCurrentMethod.Name.ToString
        Return returnGrammar
    End Function
    Public Function ChooseGrammar() As Grammar
        CheckGameState()
        Dim chooseOptions As New Choices
        For i = 1 To 4
            chooseOptions.Add(i.ToString)
        Next

        Dim chooseBuilder As New GrammarBuilder
        chooseBuilder.Append(ResChoice("CHOOSECOMMAND"))
        chooseBuilder.Append(ResChoice("CHOOSEOPTION"))
        chooseBuilder.Append(New SemanticResultKey("choose", chooseOptions))
        chooseBuilder.Append(ResChoice("CHOOSEOF"))
        chooseBuilder.Append(New SemanticResultKey("max", chooseOptions))

        Dim returnGrammar As New Grammar(chooseBuilder)
        returnGrammar.Name = Reflection.MethodBase.GetCurrentMethod.Name.ToString
        Return returnGrammar
    End Function
    Public Function EmoteGrammar() As Grammar
        Dim sayBuilder As New GrammarBuilder
        sayBuilder.Append(ResChoice("EMOTE"))
        Dim sayChoices As New Choices
        sayChoices.Add(New SemanticResultValue(ResChoice("EMOTETHANKS"), "thanks"))
        sayChoices.Add(New SemanticResultValue(ResChoice("EMOTEWELLPLAYED"), "well played"))
        sayChoices.Add(New SemanticResultValue(ResChoice("EMOTEGREETINGS"), "greetings"))
        sayChoices.Add(New SemanticResultValue(ResChoice("EMOTESORRY"), "sorry"))
        sayChoices.Add(New SemanticResultValue(ResChoice("EMOTEOOPS"), "oops"))
        sayChoices.Add(New SemanticResultValue(ResChoice("EMOTETHREATEN"), "threaten"))
        sayBuilder.Append(New SemanticResultKey("emote", sayChoices))

        Dim returnGrammar As New Grammar(sayBuilder)
        returnGrammar.Name = Reflection.MethodBase.GetCurrentMethod.Name.ToString
        Return returnGrammar
    End Function

    Private Function FriendlyHandChoices(Optional AddCard As Boolean = True) As Choices
        Dim cardHand As List(Of Entity) = GetCardsInHand()

        Dim handChoices As Choices = CreateChoicesFromEntities(cardHand)

        For i = 1 To cardHand.Count
            Dim choiceGrammar As New GrammarBuilder
            If AddCard Then _
                choiceGrammar.Append(ResChoice("CARD"))
            choiceGrammar.Append(i.ToString)
            handChoices.Add(New SemanticResultValue(choiceGrammar, SemanticEntityValue(GrammarEntityType.Card, (i))))
        Next

        Return handChoices
    End Function
    Private Function FriendlyTargetChoices() As Choices
        CheckGameState()
        Dim friendlyChoices As New Choices
        Dim friendlyMinions = GetFriendlyMinions()

        If friendlyMinions.Count > 0 Then
            friendlyChoices.Add(CreateChoicesFromEntities(friendlyMinions))

            Dim friendlyNums As New Choices
            For i = 1 To friendlyMinions.Count
                Dim friendlyNum As New GrammarBuilder
                friendlyNum.Append(ResChoice("MINION"))
                friendlyNum.Append(i.ToString)
                friendlyNums.Add(New SemanticResultValue(friendlyNum, SemanticEntityValue(GrammarEntityType.Friendly, i)))
            Next
            friendlyChoices.Add(friendlyNums)
        End If

        Dim friendlyHero As New GrammarBuilder
        friendlyHero.Append(heroNames)
        friendlyChoices.Add(New SemanticResultValue(heroNames, SemanticEntityValue(GrammarEntityType.Entity, PlayerEntity.Id)))

        Return friendlyChoices
    End Function
    Private Function OpposingTargetChoices() As Choices
        CheckGameState() ' Check IDs are in place
        Dim opposingChoices As New Choices
        Dim opposingMinions = GetOpposingMinions() ' Load Opposing minions

        If opposingMinions.Count > 0 Then
            ' Turn minion names into Choices
            opposingChoices.Add(CreateChoicesFromEntities(opposingMinions))

            ' Generate minion numbers
            Dim opposingNums As New Choices


            For i = 1 To opposingMinions.Count
                Dim opposingNum As New GrammarBuilder
                opposingNum.Append(ResChoice("MINION"))
                opposingNum.Append(i.ToString)
                opposingNums.Add(New SemanticResultValue(opposingNum, SemanticEntityValue(GrammarEntityType.Opposing, i)))
            Next
            opposingChoices.Add(opposingNums)
        End If

        opposingChoices.Add(New SemanticResultValue(heroNames, SemanticEntityValue(GrammarEntityType.Entity, OpponentEntity.Id)))

        Return opposingChoices
    End Function

    Public Function MenuGrammar() As Grammar
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
        menuChoices.Add(New SemanticResultKey("menu", "buy arena With gold"))
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
    End Function

    Private Sub StartNewGame()
        friendlyID = Nothing
        opposingID = Nothing

        If Not IsNothing(PlayerEntity) And Not IsNothing(OpponentEntity) Then
            friendlyID = PlayerEntity.GetTag(GAME_TAG.CONTROLLER)
            opposingID = OpponentEntity.GetTag(GAME_TAG.CONTROLLER)
        End If
    End Sub                                     ' Re-initalizes controller IDs
    Private Function GetCardsInHand() As List(Of Entity)
        CheckGameState()

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
        CheckGameState()

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
        CheckGameState()

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
    Private Function GetHeroPowerNames() As Choices
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
            Return heroChoice
        Else
            Return heroChoice
        End If
    End Function

    Public Sub New()
        GameEvents.OnGameStart.Add(New Action(AddressOf StartNewGame))
        StartNewGame()
    End Sub
    Private Sub CheckGameState()
        If friendlyID = 0 Or opposingID = 0 Then
            StartNewGame()
        End If
    End Sub
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
    Private Function CreateChoicesFromEntities(EntityList As List(Of Entity)) As Choices
        CheckGameState()

        ' Get a list of current cards
        Dim entityChoices As New Choices

        Dim newEntities As List(Of Entity) = EntityList.ToList


        If newEntities.Count > 0 Then
            Do
                For Each entity In newEntities
                    Dim cardName = entity.Card.Name
                    Dim cardPos = newEntities.IndexOf(entity) + 1
                    Dim cardSame = newEntities.FindAll(Function(x) x.CardId = entity.CardId)
                    Dim cardSemanticValue As String = SemanticEntityValue(GrammarEntityType.Entity, entity.Id)

                    entityChoices.Add(New GrammarBuilder(New SemanticResultValue(
                                                       cardName,
                                                       cardSemanticValue)))

                    If cardSame.Count > 1 Then
                        For Each card In cardSame
                            entityChoices.Add(New GrammarBuilder(New SemanticResultValue(
                                                               String.Format("{0} {1}", cardName, cardSame.IndexOf(card) + 1),
                                                               SemanticEntityValue(GrammarEntityType.Entity, card.Id))))
                            newEntities.Remove(card)
                        Next
                        Continue Do
                    Else
                        newEntities.Remove(entity)
                        Continue Do
                    End If
                Next
                Exit Do
            Loop
            Return entityChoices
        End If
        Return Nothing
    End Function 'Returns a list of card names from the current game

    Private Function ResChoice(Reference As String) As Choices
        Dim strResource As String = My.Resources.ResourceManager.GetString(Reference)
        Dim finalChoices As New Choices
        If Not strResource Is Nothing Then
            Dim stringChoices As String() = strResource.Split(",")

            For Each choices In stringChoices
                finalChoices.Add(choices)
            Next
        End If
        Return finalChoices
    End Function

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
    Public Function GetSemanticType(Semantic As String) As GrammarEntityType
        If IsNumeric(Semantic) Then
            Return GrammarEntityType.Entity
        End If

        Select Case Semantic.Substring(0, 1)
            Case "E"
                Return GrammarEntityType.Entity
            Case "C"
                Return GrammarEntityType.Card
            Case "F"
                Return GrammarEntityType.Friendly
            Case "O"
                Return GrammarEntityType.Opposing
            Case "H"
                Return GrammarEntityType.HeroPower
        End Select

        Return Nothing
    End Function
    Enum GrammarEntityType
        Entity
        Card
        Friendly
        Opposing
        HeroPower
    End Enum
End Class
