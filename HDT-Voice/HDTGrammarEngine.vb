Imports System.Speech.Recognition
Imports Hearthstone_Deck_Tracker
Imports Hearthstone_Deck_Tracker.API
Imports Hearthstone_Deck_Tracker.Enums
Imports Hearthstone_Deck_Tracker.Hearthstone
Imports Hearthstone_Deck_Tracker.Hearthstone.Entities

Public Class HDTGrammarEngine
    Private handCards, boardFriendly, boardOpposing As New List(Of Entity)

    Private playerID As Integer = 0
    Private opponentID As Integer = 0

    Private FriendlyNames As New Choices("my", "friendly")
    Private OpposingNames As New Choices("enemy", "opposing", "choices")


    Public Function FriendlyHand() As GrammarBuilder
        RefreshGameData()

        Dim cardGrammar As New GrammarBuilder

        If handCards.Count > 0 Then
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
            Return Nothing
        End If
    End Function
    Public Function FriendlyTargets() As GrammarBuilder
        RefreshGameData()
        ' Build the grammar for friendly minions and hero
        Dim friendlyGrammar As New GrammarBuilder ' Represents the names and numbers of minions, and the hero
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

        friendlyGrammar.Append(New SemanticResultKey("friendly", friendlyChoices))
        Return friendlyGrammar
    End Function
    Public Function OpposingTargets() As GrammarBuilder
        RefreshGameData()
        ' Build grammar for opposing minions and hero
        Dim opposingGrammar As New GrammarBuilder
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

        opposingGrammar.Append(New SemanticResultKey("opposing", New Choices(opposingChoices)))
        Return opposingGrammar
    End Function
    Public Function MulliganGrammar() As GrammarBuilder
        RefreshGameData()

        Dim mulliganBuilder As New GrammarBuilder
        Dim mulliganChoices As New Choices

        mulliganBuilder.Append(New SemanticResultKey("action", New SemanticResultValue("click", "mulligan")))
        mulliganChoices.Add("confirm")
        mulliganChoices.Add(FriendlyHand)
        mulliganBuilder.Append(New Choices(mulliganChoices, New SemanticResultValue("confirm", "mulligan")))

        Return mulliganBuilder
    End Function
    Public Function HeroPower() As GrammarBuilder
        Dim heroChoice = New Choices(New SemanticResultValue("hero power", "hero"))
        'Attempt to read active hero power name

        Dim heroPowerEntity As Entity = Nothing

        heroPowerEntity = Entities.FirstOrDefault(Function(x)
                                                      Dim cardType = x.GetTag(GAME_TAG.CARDTYPE)
                                                      Dim cardController = x.GetTag(GAME_TAG.CONTROLLER)
                                                      Dim cardInPlay = x.IsInPlay

                                                      If cardType = Hearthstone.TAG_CARDTYPE.HERO_POWER And
                                                                cardController = playerID And
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
    End Function
    ' PlayCard
    ' AttackTarget
    ' HeroPowerTarget
    ' ClickTarget
    ' TargetTarget

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
    End Function



    Private Sub RefreshGameData()
        If Not IsNothing(PlayerEntity) Then _
            playerID = PlayerEntity.GetTag(GAME_TAG.CONTROLLER)
        If Not IsNothing(OpponentEntity) Then _
            opponentID = OpponentEntity.GetTag(GAME_TAG.CONTROLLER)

        'build list of cards in hand
        handCards.Clear()

        For Each e In Entities
            If e.IsInHand And e.GetTag(GAME_TAG.CONTROLLER) = playerID Then
                handCards.Add(e)
            End If
        Next

        ' sort cards by position in hand
        handCards.Sort(Function(e1 As Entity, e2 As Entity)
                           Return e1.GetTag(GAME_TAG.ZONE_POSITION).CompareTo(e2.GetTag(GAME_TAG.ZONE_POSITION))
                       End Function)

        ' build list of minions on board
        boardFriendly.Clear()
        boardOpposing.Clear()

        For Each e In Entities
            If e.IsInPlay And e.IsMinion Then
                If e.IsControlledBy(playerID) Then
                    boardFriendly.Add(e)
                ElseIf e.IsControlledBy(opponentID) Then
                    boardOpposing.Add(e)
                End If
            End If
        Next

        ' sort by position on board
        boardFriendly.Sort(Function(e1 As Entity, e2 As Entity)
                               Return e1.GetTag(GAME_TAG.ZONE_POSITION).CompareTo(e2.GetTag(GAME_TAG.ZONE_POSITION))
                           End Function)

        boardOpposing.Sort(Function(e1 As Entity, e2 As Entity)
                               Return e1.GetTag(GAME_TAG.ZONE_POSITION).CompareTo(e2.GetTag(GAME_TAG.ZONE_POSITION))
                           End Function)
    End Sub 'Rebuilds data for cards in hand and on board

    Private ReadOnly Property Entities As Entity()
        Get
            ' Clone entities from game and return as array
            Return Helper.DeepClone(Game.Entities).Values.ToArray
        End Get
    End Property ' The list of entities for the current game
    Private ReadOnly Property PlayerEntity As Entity
        Get
            ' Return the Entity representing the player
            Try
                Return Entities.First(Function(x) x.IsPlayer())
            Catch ex As Exception
                Return Nothing
            End Try
        End Get
    End Property ' The player's entity
    Private ReadOnly Property OpponentEntity As Entity
        Get
            ' Return the Entity representing the player
            Try
                Return Entities.First(Function(x) x.IsOpponent())
            Catch ex As Exception
                Return Nothing
            End Try
        End Get
    End Property ' The opponent entity
End Class
