import express, { Request, Response } from 'express';
import { getPool } from '../middleware/database';
import { everyoneSubmitted, getByCode, insertGroup, insertPlayerToGroup, submitDungeon } from './groups/groups.queries';
import crypto from 'crypto';
import { DatabaseError } from 'pg';

export const groupsRouter = express.Router();

groupsRouter.get('/:code', async (req: Request, res: Response) => {
    try {
        await getPool().getCon(async (c) => {
            const y = req.params['code'];
            const z = await getByCode.run({ code: y }, c);
            res.status(200).send(z);
        });
    } catch (e) {
        const x = e as Error;
        res.status(500).send(x['message']);
    }
});
groupsRouter.post('/:code', async (req: Request, res: Response) => {
    try {
        getPool().getCon(async (c) => {
            const code = req.params['code'];
            const playerCode = crypto.randomBytes(6).toString('hex').slice(0, 6);
            await insertPlayerToGroup.run({ dungeonCode: code, code: playerCode }, c);
            res.send({ success: true, id: playerCode });
        });
    } catch (e) {
        const x = e as Error;
        res.status(500).send(x['message']);
    }
});
groupsRouter.post('/', async (req: Request, res: Response) => {
    try {
        getPool().getCon(async (c) => {
            while (true) {
                const code = crypto.randomBytes(6).toString('hex').slice(0, 6);
                try {
                    await insertGroup.run({ code }, c);
                    res.status(201).send({ success: true, code });
                } catch (e) {
                    if (e && typeof e == 'object' && 'code' in e) {
                        const x = e as DatabaseError;
                        if (x.code != '23505') {
                            throw e;
                        }
                    }
                    throw e;
                }
            }
        });
    } catch (e) {
        const x = e as Error;
        res.status(500).send(x['message']);
    }
});
groupsRouter.post('/:code/players/:player_code', async (req: Request, res: Response) => {
    try {
        getPool().getCon(async (c) => {
            await submitDungeon.run(
                { dungeon_code: req.params['code'], player_code: req.params['player_code'], dungeon: req.body },
                c,
            );
        });
        res.send({ success: true });
    } catch (e) {
        const x = e as Error;
        res.status(500).send(x['message']);
    }
});

groupsRouter.get(`/:code/is_everyone_done`, async (req: Request, res: Response) => {
    try {
        getPool().getCon(async (c) => {
            const r = await everyoneSubmitted.run({ code: req.params['code'] }, c);
            res.send({ is_done: r[0] });
        });
        res.send({ success: true });
    } catch (e) {
        const x = e as Error;
        res.status(500).send(x['message']);
    }
});
groupsRouter.post('/:code/players/:player_id/finished_dungeon', async (req: Request, res: Response) => {
    try {
    } catch (e) {
        const x = e as Error;
        res.status(500).send(x['message']);
    }
});
