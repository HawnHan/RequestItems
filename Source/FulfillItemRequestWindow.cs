﻿using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace ItemRequests
{
    class FulfillItemRequestWindow : Window
    {
        private Pawn playerPawn;
        private Pawn traderPawn;
        private Vector2 scrollPosition = Vector2.zero;
        private List<RequestItem> requestedItems;        
        private const float offsetFromRight = 100;
        private const float offsetFromBottom = 90;

        public FulfillItemRequestWindow(Pawn playerPawn, Pawn traderPawn)
        {
            this.playerPawn = playerPawn;
            this.traderPawn = traderPawn;
            this.requestedItems = RequestSession.GetOpenDealWith(traderFaction).GetRequestedItems();
        }

        private Faction traderFaction => traderPawn.Faction;

        public override Vector2 InitialSize => new Vector2(500, 700);

        public override void DoWindowContents(Rect inRect)
        {
            Vector2 contentMargin = new Vector2(12, 18);
            string title = "Review Requested Items";
            string closeString = "Trade";

            // Begin Window group
            GUI.BeginGroup(inRect);

            // Draw the names of negotiator and factions
            inRect = inRect.AtZero();
            float x = contentMargin.x;
            float headerRowHeight = 35f;
            Rect headerRowRect = new Rect(x, contentMargin.y, inRect.width - x, headerRowHeight);
            Rect titleArea = new Rect(x, 0, headerRowRect.width, headerRowRect.height);
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Medium;
            Widgets.Label(titleArea, title);

            Text.Font = GameFont.Small;
            float constScrollbarSize = 16;
            float rowHeight = 30;
            float cumulativeContentHeight = 6f + requestedItems.Count * rowHeight;
            Rect mainRect = new Rect(x, headerRowRect.y + headerRowRect.height + 10, inRect.width - x * 2, inRect.height - offsetFromBottom - headerRowRect.y - headerRowRect.height);
            Rect scrollRect = new Rect(mainRect.x, 0, mainRect.width - constScrollbarSize, cumulativeContentHeight);
            float bottom = scrollPosition.y - 30f;
            float top = scrollPosition.y + mainRect.height;
            float y = 6f;
            int counter;

            Widgets.BeginScrollView(mainRect, ref scrollPosition, scrollRect, true);

            for (int i = 0; i < requestedItems.Count; i++)
            {
                counter = i;
                if (y > bottom && y < top)
                {
                    Rect rect = new Rect(mainRect.x, y, scrollRect.width, 30f);
                    DrawRequestedItem(rect, requestedItems[i], counter);
                }
                y += 30f;
            }

            Widgets.EndScrollView();

            float horizontalLineY = inRect.height - offsetFromBottom;
            Widgets.DrawLineHorizontal(x, horizontalLineY, inRect.width - contentMargin.x * 2);

            // Draw total
            Text.Anchor = TextAnchor.MiddleRight;
            Rect totalStringRect = new Rect(x, horizontalLineY, scrollRect.width - offsetFromRight - contentMargin.x, rowHeight);
            Widgets.Label(totalStringRect, "Total");            
            Widgets.DrawLineVertical(scrollRect.width - offsetFromRight, 0, rowHeight);
            Rect totalPriceRect = new Rect(scrollRect.width - offsetFromRight, horizontalLineY, offsetFromRight - 10, rowHeight);
            Widgets.Label(totalPriceRect, RequestSession.GetOpenDealWith(traderFaction).TotalRequestedValue.ToStringMoney("F2"));
                        
            Text.Anchor = TextAnchor.MiddleLeft;
            Rect closeButtonArea = new Rect(x, inRect.height - contentMargin.y * 2, 100, 50);
            if (Widgets.ButtonText(closeButtonArea, closeString, false))
            {
                CloseButtonPressed();
            }

            GenUI.ResetLabelAlign();
            GUI.EndGroup();
        }

        private void DrawRequestedItem(Rect rowRect, RequestItem requested, int index)
        {
            Text.Font = GameFont.Small;
            float price = requested.pricePerItem * requested.amount;
            if (index % 2 == 1)
            {
                Widgets.DrawLightHighlight(rowRect);
            }

            GUI.BeginGroup(rowRect);


            // Draw item icon
            float x = 0;
            float iconSize = 27;
            Text.Anchor = TextAnchor.MiddleLeft;
            Rect iconArea = new Rect(x, 0, iconSize, iconSize);
            Widgets.ThingIcon(iconArea, requested.item.AnyThing.def);

            x += iconSize * (iconSize / 2);

            // Draw item name
            Rect itemNameArea = new Rect(x, 0, rowRect.width - offsetFromRight - x, rowRect.height);
            string itemTitle = requested.item.AnyThing.LabelCapNoCount + " x" + requested.amount;
            Widgets.Label(itemNameArea, itemTitle);

            x = rowRect.width - offsetFromRight;
            Widgets.DrawLineVertical(x, 0, rowRect.height);

            x += 10;
            Text.Anchor = TextAnchor.MiddleRight;
            Rect itemPriceArea = new Rect(x, 0, offsetFromRight - 10, rowRect.height);
            Widgets.Label(itemPriceArea, price.ToStringMoney("F2"));

            GUI.EndGroup();
        }
    
        private void CloseButtonPressed()
        {
            Close(true);
            Log.Message("close button pressed");

            float totalRequestedValue = RequestSession.GetOpenDealWith(traderFaction).TotalRequestedValue;
            if (playerPawn.Map.resourceCounter.Silver < totalRequestedValue)
            {
                Log.Message("Colony didn't have enough silver");
                Lord lord = traderPawn.GetLord();
                lord.ReceiveMemo(LordJob_FulfillItemRequest.MemoOnUnfulfilled);
            }
            else
            {
                ITrader trader = traderPawn as ITrader;
                if (trader == null)
                {
                    Log.Error("Trader pawn unable to be cast to ITrader!");
                    return;
                }

                foreach (RequestItem requested in requestedItems)
                {
                    Thing thing = ThingMaker.MakeThing(requested.item.ThingDef, requested.item.StuffDef);
                    


                    trader.GiveSoldThingToPlayer(thing, requested.amount, playerPawn);
                    Log.Message("Just gave " + thing.LabelCapNoCount + " x" + requested.amount + " to player");
                }

                Log.Message("Trade successful!");
                traderFaction.Notify_PlayerTraded(totalRequestedValue, playerPawn);
                TaleRecorder.RecordTale(TaleDefOf.TradedWith, new object[]
                {
                        playerPawn,
                        traderPawn
                });

                Lord lord = traderPawn.GetLord();
                lord.ReceiveMemo(LordJob_FulfillItemRequest.MemoOnFulfilled);
            }

            //RequestSession.CloseOpenDealWith(traderFaction);
        }
    }
}
