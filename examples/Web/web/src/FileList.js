import React from 'react';
import { formatSeconds, formatBytes } from './util';

import { 
    Header, 
    Table, 
    Icon, 
    List, 
    Checkbox 
} from 'semantic-ui-react';

const FileList = ({ files }) => (
    <div>
        <Header 
            size='small' 
            className='filelist-header'
        >
            <Icon name='folder'/>Folder/name/here
        </Header>
        <List>
            <List.Item>
            <Table singleLine>
                <Table.Header>
                    <Table.Row>
                        <Table.HeaderCell><Checkbox label=''/></Table.HeaderCell>
                        <Table.HeaderCell>File</Table.HeaderCell>
                        <Table.HeaderCell>Bitrate</Table.HeaderCell>
                        <Table.HeaderCell>Length</Table.HeaderCell>
                        <Table.HeaderCell>Size</Table.HeaderCell>
                    </Table.Row>
                </Table.Header>                                
                <Table.Body>
                    {files.map(f => 
                        <Table.Row>
                            <Table.Cell><Checkbox label=''/></Table.Cell>
                            <Table.Cell>{f.filename.split('\\').pop().split('/').pop()}</Table.Cell>
                            <Table.Cell>{f.bitRate}</Table.Cell>
                            <Table.Cell>{formatSeconds(f.length)}</Table.Cell>
                            <Table.Cell>{formatBytes(f.size)}</Table.Cell>
                        </Table.Row>
                    )}
                </Table.Body>
            </Table>
            </List.Item>
        </List>
    </div>
);

export default FileList;
