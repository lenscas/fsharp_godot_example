import express from 'express';
import helmet from 'helmet';
import { groupsRouter } from './routes/groups';

const app = express();
app.use(helmet());

app.use(express.json());

app.use('/groups', groupsRouter);

app.listen(8080, () => {
    console.log('server started');
});
