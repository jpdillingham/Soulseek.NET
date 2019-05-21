import React from 'react';

import { formatBytes, getFileName } from './util';

import { 
    Header, 
    Table, 
    Icon, 
    List, 
    Progress
} from 'semantic-ui-react';

const DownloadList = ({ directoryName, files }) => (
    <div>
        <Header 
            size='small' 
            className='filelist-header'
        >
            <Icon name='folder'/>{directoryName}
        </Header>
        <List>
            <List.Item>
            <Table>
                <Table.Header>
                    <Table.Row>
                        <Table.HeaderCell>File</Table.HeaderCell>
                        <Table.HeaderCell>Size</Table.HeaderCell>
                        <Table.HeaderCell>Progress</Table.HeaderCell>
                    </Table.Row>
                </Table.Header>                                
                <Table.Body>
                    {files.sort((a, b) => getFileName(a.filename).localeCompare(getFileName(b.filename))).map((f, i) => 
                        <Table.Row key={i}>
                            <Table.Cell>{getFileName(f.filename)}</Table.Cell>
                            <Table.Cell>{formatBytes(f.bytesDownloaded) + '/' + formatBytes(f.size)}</Table.Cell>
                            <Table.Cell>
                                <Progress style={{ margin: 0}} percent={Math.round(f.percentComplete)} progress color='green'/>
                            </Table.Cell>
                        </Table.Row>
                    )}
                </Table.Body>
            </Table>
            </List.Item>
        </List>
    </div>
);

export default DownloadList;
