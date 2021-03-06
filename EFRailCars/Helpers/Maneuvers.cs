﻿using EFRailCars.Entities;
using EFRailCars.Railcars;
using Logs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EFRailCars.Helpers
{
    public enum Side { Empty = -1, Even = 0, Odd = 1 } // Четная, Нечетная
    public enum errorManeuvers : int
    {
        global = -1,
        no_string_wagon_operations = -2,
        way_not_station = -3,
        no_close_old_maneuvers = -4,
        no_create_new_maneuvers = -5,
        no_list_wagon_operations = -6,
    }

    public class Maneuvers
    {
        private eventID eventID = eventID.EFRailCars_Helpers_Maneuvers;
        private RC_VagonsOperations rc_vo = new RC_VagonsOperations();
        private RC_Ways rc_ws = new RC_Ways();

        public Maneuvers() { }

        #region Выполнение маневра (с учетом правил, заложен механизм без учета правил)

        /// <summary>
        /// Сделать маневр по вагону
        /// </summary>
        /// <param name="wag_oper"></param>
        /// <param name="side_station"></param>
        /// <returns></returns>
        public int ManeuverCar(VAGON_OPERATIONS wag_oper, Side side_station)
        {
            if (wag_oper == null) return (int)errorManeuvers.no_string_wagon_operations;
            // Проверка путь на который маневр пренадлежит этой станции
            if (wag_oper.id_stat != rc_ws.GetIDStationOfWay((int)wag_oper.lock_id_way)) return (int)errorManeuvers.way_not_station;
            try
            {
                int position = 1;
                if ((int)side_station == wag_oper.lock_side)
                {
                    int p = (int)wag_oper.lock_id_way;
                    rc_vo.OffSetCars(p, 2);// Сместить вагоны на пути
                }
                else
                {
                    rc_vo.OffSetCars((int)wag_oper.lock_id_way, 1);// Выставить по порядку
                    // добавить вагон зади
                    int? num = rc_vo.MaxPositionWay((int)wag_oper.lock_id_way);
                    if (num != null)
                    { position = (int)num + 1; }
                }
                VAGON_OPERATIONS new_vagon = new VAGON_OPERATIONS()
                {
                    id_oper = 0,
                    dt_uz = wag_oper.dt_uz,
                    dt_amkr = wag_oper.dt_amkr,
                    dt_out_amkr = null,
                    n_natur = wag_oper.n_natur,
                    id_vagon = wag_oper.id_vagon,
                    id_stat = wag_oper.id_stat,
                    dt_from_stat = null,
                    dt_on_stat = wag_oper.dt_on_stat,
                    id_way = wag_oper.lock_id_way,
                    dt_from_way = null,
                    dt_on_way = DateTime.Now,
                    num_vag_on_way = position,
                    is_present = 1,
                    id_locom = wag_oper.id_locom,
                    id_locom2 = wag_oper.id_locom2,
                    id_cond2 = wag_oper.id_cond2,
                    id_gruz = wag_oper.id_gruz,
                    id_gruz_amkr = wag_oper.id_gruz_amkr,
                    id_shop_gruz_for = wag_oper.id_shop_gruz_for,
                    weight_gruz = wag_oper.weight_gruz,
                    id_tupik = wag_oper.id_tupik,
                    id_nazn_country = wag_oper.id_nazn_country,
                    id_gdstait = wag_oper.id_gdstait,
                    id_cond = wag_oper.id_cond,
                    note = wag_oper.note,
                    is_hist = 0,
                    id_oracle = wag_oper.id_oracle,
                    lock_id_way = null,
                    lock_order = null,
                    lock_side = null,
                    lock_id_locom = null,
                    st_lock_id_stat = null,
                    st_lock_order = null,
                    st_lock_train = null,
                    st_lock_side = null,
                    st_gruz_front = null,
                    st_shop = null,
                    oracle_k_st = wag_oper.oracle_k_st,
                    st_lock_locom1 = null,
                    st_lock_locom2 = null,
                    id_oper_parent = wag_oper.id_oper,
                    grvu_SAP = wag_oper.grvu_SAP,
                    ngru_SAP = wag_oper.ngru_SAP,
                    id_ora_23_temp = wag_oper.id_ora_23_temp,
                    IDSostav = wag_oper.IDSostav,
                    num_vagon = wag_oper.num_vagon
                };
                int res = rc_vo.SaveVagonsOperations(new_vagon);
                if (res > 0)
                {
                    // Закроем старую запись
                    wag_oper.lock_id_way = null;
                    wag_oper.lock_order = null;
                    wag_oper.lock_side = null;
                    wag_oper.lock_id_locom = null;
                    wag_oper.is_hist = 1;
                    wag_oper.is_present = 0;
                    int res_old = rc_vo.SaveVagonsOperations(wag_oper);
                    if (res_old > 0) return 1;
                    if (res_old < 0)
                    {
                        LogRW.LogError(String.Format("[Maneuvers.ManeuverCar]: Ошибка, номер вагона {0}, id_oper {1}, код ошибки {2}", wag_oper.num_vagon, wag_oper.id_oper, errorManeuvers.no_close_old_maneuvers.ToString()), eventID);
                        return (int)errorManeuvers.no_close_old_maneuvers;
                    }
                }
                if (res < 0)
                {
                    LogRW.LogError(String.Format("[Maneuvers.ManeuverCar]: Ошибка, номер вагона {0}, id_oper {1}, код ошибки {2}", new_vagon.num_vagon, wag_oper.id_oper, errorManeuvers.no_create_new_maneuvers.ToString()), eventID);
                    return (int)errorManeuvers.no_create_new_maneuvers;
                }
                return 0;
            }
            catch (Exception e)
            {
                LogRW.LogError(String.Format("[Maneuvers.ManeuverCar]: Ошибка, источник: {0}, № {1}, описание:  {2}", e.Source, e.HResult, e.Message), this.eventID);
                return (int)errorManeuvers.global;
            }
        }
        /// <summary>
        ///  Сделать маневр по группе вагонов
        /// </summary>
        /// <param name="list"></param>
        /// <param name="side_station"></param>
        /// <returns></returns>
        public int ManeuverCars(List<VAGON_OPERATIONS> list, Side side_station)
        {
            if (list.Count() == 0) return (int)errorManeuvers.no_list_wagon_operations;
            int result = 0;
            try
            {
                var group_site = list.GroupBy(o => o.lock_side);

                foreach (var group_wag in group_site.ToList())
                {
                    List<VAGON_OPERATIONS> list_wag = new List<VAGON_OPERATIONS>();
                    list_wag = list.OrderByDescending(o => o.lock_order).ToList();
                    //TODO: Включить если ненадо учитывать правило следования вагонов
                    //list_wag = group_wag.Key == (int)side_station ? list.OrderByDescending(o => o.lock_order).ToList() : list.OrderBy(o => o.lock_order).ToList();
                    foreach (VAGON_OPERATIONS wag in list_wag)
                    {
                        int res = ManeuverCar(wag, side_station);
                        if (res > 0) result++;
                    }
                }
                return result;
            }
            catch (Exception e)
            {
                LogRW.LogError(String.Format("[Maneuvers.ManeuverCars(1)]: Ошибка, источник: {0}, № {1}, описание:  {2}", e.Source, e.HResult, e.Message), this.eventID);
                return (int)errorManeuvers.global;
            }
        }
        /// <summary>
        /// сделать маневр по вагонам стоящим на указаном пути
        /// </summary>
        /// <param name="way"></param>
        /// <returns></returns>
        public int ManeuverCars(int way, Side side_station)
        {
            int result = 0;
            // Получить вагоны и отгрупировать их по путям отправки
            List<IGrouping<int?, VAGON_OPERATIONS>> group_list = new List<IGrouping<int?, VAGON_OPERATIONS>>();
            group_list = rc_vo.GetWagonsOfWay(way).Where(o => o.lock_id_way != null).GroupBy(o => o.lock_id_way).ToList();

            //var group_list = rc_vo.GetWagonsOfWay(way).Where(o => o.lock_id_way != null).GroupBy(o => o.lock_id_way).ToList();
            try
            {
                foreach (IGrouping<int?, VAGON_OPERATIONS> group_wag in group_list)
                {
                    List<VAGON_OPERATIONS> list_wag = new List<VAGON_OPERATIONS>();
                    list_wag = group_wag.OrderBy(o => o.lock_order).ToList();
                    int res = ManeuverCars(list_wag, side_station);
                    if (res > 0) result += res;
                }
                rc_vo.OffSetCars(way, 1);
                return result;

            }
            catch (Exception e)
            {
                LogRW.LogError(String.Format("[Maneuvers.ManeuverCars(2)]: Ошибка, источник: {0}, № {1}, описание:  {2}", e.Source, e.HResult, e.Message), this.eventID);
                return (int)errorManeuvers.global;
            }
        }
        #endregion

        #region Действия над строками вагонов
        /// <summary>
        /// Установить вагон с признаком для маневра
        /// </summary>
        /// <param name="wag"></param>
        /// <param name="lock_id_way"></param>
        /// <param name="lock_order"></param>
        /// <param name="lock_side"></param>
        /// <param name="lock_id_locom"></param>
        /// <param name="dt_from_way"></param>
        /// <returns></returns>
        public int AddCancelManeuverCar(int id_oper, int? lock_id_way, int? lock_order, int? lock_side, int? lock_id_locom, DateTime? dt_from_way)
        {
            VAGON_OPERATIONS wag = rc_vo.GetVagonsOperations(id_oper);
            return AddCancelManeuverCar(wag, lock_id_way, lock_order, lock_side, lock_id_locom, dt_from_way);
        }
        /// <summary>
        /// Установить вагон с признаком для маневра
        /// </summary>
        /// <param name="wag"></param>
        /// <param name="lock_id_way"></param>
        /// <param name="lock_order"></param>
        /// <param name="lock_side"></param>
        /// <param name="lock_id_locom"></param>
        /// <param name="dt_from_way"></param>
        /// <returns></returns>
        public int AddCancelManeuverCar(VAGON_OPERATIONS wag, int? lock_id_way, int? lock_order, int? lock_side, int? lock_id_locom, DateTime? dt_from_way)
        {
            if (wag == null) return (int)errorManeuvers.no_string_wagon_operations;
            wag.lock_id_way = lock_id_way;
            wag.lock_order = lock_order;
            wag.lock_side = lock_side;
            wag.lock_id_locom = lock_id_locom;
            wag.dt_from_way = dt_from_way;
            return rc_vo.SaveVagonsOperations(wag);
        }
        /// <summary>
        /// Отменить маневры
        /// </summary>
        /// <param name="way"></param>
        /// <returns></returns>
        public int CancelManeuverCars(int way) 
        {
            int result = 0;
            List<VAGON_OPERATIONS> list = rc_vo.GetWagonsOfWay(way).Where(o => o.lock_id_way != null).ToList();
            foreach (VAGON_OPERATIONS wag in list) 
            {
                int res = AddCancelManeuverCar(wag, null, null, null, null, null);
                if (res > 0) result++;
            }
            return result;
        }
        /// <summary>
        /// Отменить маневр
        /// </summary>
        /// <param name="id_oper"></param>
        /// <returns></returns>
        public int CancelManeuverCar(int id_oper)
        {
            int res = AddCancelManeuverCar(id_oper, null, null, null, null, null);
            return res;
        }
        /// <summary>
        /// Отменить маневры по всей станции
        /// </summary>
        /// <param name="id_stat"></param>
        /// <returns></returns>
        public int CancelManeuverCarsOfSatation(int id_stat) 
        {
            int result = 0;
            List<VAGON_OPERATIONS> list = rc_vo.GetWagonsOfStation(id_stat).Where(o => o.lock_id_way != null).ToList();
            foreach (VAGON_OPERATIONS wag in list) 
            {
                int res = AddCancelManeuverCar(wag, null, null, null, null, null);
                if (res > 0) result++;
            }
            return result;
        }

        #endregion

    }
}
