/** Types generated for queries found in "src/routes/groups/groups.sql" */
import { PreparedQuery } from '@pgtyped/query';

export type Json = null | boolean | number | string | Json[] | { [key: string]: Json };

/** 'GetByCode' parameters type */
export interface IGetByCodeParams {
  code: string | null | void;
}

/** 'GetByCode' return type */
export interface IGetByCodeResult {
  dungeon: Json | null;
  player_number: string | null;
}

/** 'GetByCode' query type */
export interface IGetByCodeQuery {
  params: IGetByCodeParams;
  result: IGetByCodeResult;
}

const getByCodeIR: any = {"name":"GetByCode","params":[{"name":"code","required":false,"transform":{"type":"scalar"},"codeRefs":{"used":[{"a":97,"b":100,"line":4,"col":22}]}}],"usedParamSet":{"code":true},"statement":{"body":"SELECT dungeon,player_number\nFROM players_with_count\nWHERE dungeon_code = :code\nORDER BY player_number","loc":{"a":22,"b":123,"line":2,"col":0}}};

/**
 * Query generated from SQL:
 * ```
 * SELECT dungeon,player_number
 * FROM players_with_count
 * WHERE dungeon_code = :code
 * ORDER BY player_number
 * ```
 */
export const getByCode = new PreparedQuery<IGetByCodeParams,IGetByCodeResult>(getByCodeIR);


/** 'InsertPlayerToGroup' parameters type */
export interface IInsertPlayerToGroupParams {
  code: string | null | void;
  dungeonCode: string | null | void;
}

/** 'InsertPlayerToGroup' return type */
export type IInsertPlayerToGroupResult = void;

/** 'InsertPlayerToGroup' query type */
export interface IInsertPlayerToGroupQuery {
  params: IInsertPlayerToGroupParams;
  result: IInsertPlayerToGroupResult;
}

const insertPlayerToGroupIR: any = {"name":"InsertPlayerToGroup","params":[{"name":"code","required":false,"transform":{"type":"scalar"},"codeRefs":{"used":[{"a":207,"b":210,"line":9,"col":12}]}},{"name":"dungeonCode","required":false,"transform":{"type":"scalar"},"codeRefs":{"used":[{"a":248,"b":258,"line":9,"col":53}]}}],"usedParamSet":{"code":true,"dungeonCode":true},"statement":{"body":"insert into players (group_id,code)\nselect id, :code as code from dungeons where code = :dungeonCode","loc":{"a":159,"b":258,"line":8,"col":0}}};

/**
 * Query generated from SQL:
 * ```
 * insert into players (group_id,code)
 * select id, :code as code from dungeons where code = :dungeonCode
 * ```
 */
export const insertPlayerToGroup = new PreparedQuery<IInsertPlayerToGroupParams,IInsertPlayerToGroupResult>(insertPlayerToGroupIR);


/** 'InsertGroup' parameters type */
export interface IInsertGroupParams {
  code: string | null | void;
}

/** 'InsertGroup' return type */
export type IInsertGroupResult = void;

/** 'InsertGroup' query type */
export interface IInsertGroupQuery {
  params: IInsertGroupParams;
  result: IInsertGroupResult;
}

const insertGroupIR: any = {"name":"InsertGroup","params":[{"name":"code","required":false,"transform":{"type":"scalar"},"codeRefs":{"used":[{"a":323,"b":326,"line":13,"col":9}]}}],"usedParamSet":{"code":true},"statement":{"body":"INSERT INTO dungeons (code)\nVALUES (:code)","loc":{"a":286,"b":327,"line":12,"col":0}}};

/**
 * Query generated from SQL:
 * ```
 * INSERT INTO dungeons (code)
 * VALUES (:code)
 * ```
 */
export const insertGroup = new PreparedQuery<IInsertGroupParams,IInsertGroupResult>(insertGroupIR);


/** 'SubmitDungeon' parameters type */
export interface ISubmitDungeonParams {
  dungeon: Json | null | void;
  dungeon_code: string | null | void;
  player_code: string | null | void;
}

/** 'SubmitDungeon' return type */
export type ISubmitDungeonResult = void;

/** 'SubmitDungeon' query type */
export interface ISubmitDungeonQuery {
  params: ISubmitDungeonParams;
  result: ISubmitDungeonResult;
}

const submitDungeonIR: any = {"name":"SubmitDungeon","params":[{"name":"dungeon","required":false,"transform":{"type":"scalar"},"codeRefs":{"used":[{"a":385,"b":391,"line":17,"col":13}]}},{"name":"dungeon_code","required":false,"transform":{"type":"scalar"},"codeRefs":{"used":[{"a":400,"b":411,"line":18,"col":7},{"a":563,"b":574,"line":25,"col":27}]}},{"name":"player_code","required":false,"transform":{"type":"scalar"},"codeRefs":{"used":[{"a":600,"b":610,"line":26,"col":24}]}}],"usedParamSet":{"dungeon":true,"dungeon_code":true,"player_code":true},"statement":{"body":"UPDATE players\nSET dungeon=:dungeon\nWHERE :dungeon_code IN (\n    SELECT\n        dungeons.code\n    FROM dungeons\n    INNER JOIN players\n    ON \n    \tplayers.group_id = dungeons.id\n    WHERE dungeons.code = :dungeon_code\n    and players.code = :player_code\n    LIMIT 1\n)","loc":{"a":357,"b":624,"line":16,"col":0}}};

/**
 * Query generated from SQL:
 * ```
 * UPDATE players
 * SET dungeon=:dungeon
 * WHERE :dungeon_code IN (
 *     SELECT
 *         dungeons.code
 *     FROM dungeons
 *     INNER JOIN players
 *     ON 
 *     	players.group_id = dungeons.id
 *     WHERE dungeons.code = :dungeon_code
 *     and players.code = :player_code
 *     LIMIT 1
 * )
 * ```
 */
export const submitDungeon = new PreparedQuery<ISubmitDungeonParams,ISubmitDungeonResult>(submitDungeonIR);


/** 'EveryoneSubmitted' parameters type */
export interface IEveryoneSubmittedParams {
  code: string | null | void;
}

/** 'EveryoneSubmitted' return type */
export interface IEveryoneSubmittedResult {
  isdone: boolean | null;
}

/** 'EveryoneSubmitted' query type */
export interface IEveryoneSubmittedQuery {
  params: IEveryoneSubmittedParams;
  result: IEveryoneSubmittedResult;
}

const everyoneSubmittedIR: any = {"name":"EveryoneSubmitted","params":[{"name":"code","required":false,"transform":{"type":"scalar"},"codeRefs":{"used":[{"a":955,"b":958,"line":41,"col":25},{"a":1215,"b":1218,"line":52,"col":27}]}}],"usedParamSet":{"code":true},"statement":{"body":"SELECT \n    done_count.done_count = total_count.total_count as isDone\nFROM dungeons\nINNER JOIN (\n    SELECT\n        count(*) as done_count,\n        dungeons.id\n    FROM players\n    INNER JOIN dungeons\n    on dungeons.id = players.group_id\n    WHERE players.dungeon is null\n    AND dungeons.code = :code\n    GROUP BY dungeons.id\n) as done_count\nON done_count.id = dungeons.id\nINNER JOIN (\n    SELECT\n        count(*) as total_count,\n        dungeons.id\n    FROM players\n    INNER JOIN dungeons\n    ON dungeons.id = players.group_id\n    WHERE dungeons.code = :code\n    GROUP BY dungeons.id\n) as total_count\nON total_count.id = dungeons.id","loc":{"a":657,"b":1292,"line":30,"col":0}}};

/**
 * Query generated from SQL:
 * ```
 * SELECT 
 *     done_count.done_count = total_count.total_count as isDone
 * FROM dungeons
 * INNER JOIN (
 *     SELECT
 *         count(*) as done_count,
 *         dungeons.id
 *     FROM players
 *     INNER JOIN dungeons
 *     on dungeons.id = players.group_id
 *     WHERE players.dungeon is null
 *     AND dungeons.code = :code
 *     GROUP BY dungeons.id
 * ) as done_count
 * ON done_count.id = dungeons.id
 * INNER JOIN (
 *     SELECT
 *         count(*) as total_count,
 *         dungeons.id
 *     FROM players
 *     INNER JOIN dungeons
 *     ON dungeons.id = players.group_id
 *     WHERE dungeons.code = :code
 *     GROUP BY dungeons.id
 * ) as total_count
 * ON total_count.id = dungeons.id
 * ```
 */
export const everyoneSubmitted = new PreparedQuery<IEveryoneSubmittedParams,IEveryoneSubmittedResult>(everyoneSubmittedIR);


/** 'FinishedDungeon' parameters type */
export interface IFinishedDungeonParams {
  dungeon_code: string | null | void;
  player_code: string | null | void;
}

/** 'FinishedDungeon' return type */
export type IFinishedDungeonResult = void;

/** 'FinishedDungeon' query type */
export interface IFinishedDungeonQuery {
  params: IFinishedDungeonParams;
  result: IFinishedDungeonResult;
}

const finishedDungeonIR: any = {"name":"finished_dungeon","params":[{"name":"dungeon_code","required":false,"transform":{"type":"scalar"},"codeRefs":{"used":[{"a":1364,"b":1375,"line":60,"col":7},{"a":1527,"b":1538,"line":67,"col":27}]}},{"name":"player_code","required":false,"transform":{"type":"scalar"},"codeRefs":{"used":[{"a":1564,"b":1574,"line":68,"col":24}]}}],"usedParamSet":{"dungeon_code":true,"player_code":true},"statement":{"body":"UPDATE players\nSET dungeon=null\nWHERE :dungeon_code IN (\n    SELECT\n        dungeons.code\n    FROM dungeons\n    INNER JOIN players\n    ON \n    \tplayers.group_id = dungeons.id\n    WHERE dungeons.code = :dungeon_code\n    and players.code = :player_code\n    LIMIT 1\n)","loc":{"a":1325,"b":1588,"line":58,"col":0}}};

/**
 * Query generated from SQL:
 * ```
 * UPDATE players
 * SET dungeon=null
 * WHERE :dungeon_code IN (
 *     SELECT
 *         dungeons.code
 *     FROM dungeons
 *     INNER JOIN players
 *     ON 
 *     	players.group_id = dungeons.id
 *     WHERE dungeons.code = :dungeon_code
 *     and players.code = :player_code
 *     LIMIT 1
 * )
 * ```
 */
export const finishedDungeon = new PreparedQuery<IFinishedDungeonParams,IFinishedDungeonResult>(finishedDungeonIR);


