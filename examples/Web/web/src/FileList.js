import React from 'react';

import { formatSeconds, formatBytes, getFileName } from './util';

import { 
    Header, 
    Table, 
    Icon, 
    List, 
    Checkbox,
    Accordion
} from 'semantic-ui-react';

const FileList = ({ directoryName, files, onSelectionChange, disabled }) => (
    <div>
        <Header 
            size='small' 
            className='filelist-header'
        >
            <Icon name='folder'/>{directoryName}
        </Header>
        <Accordion>
            <Accordion.Title active={true}>
                <Icon name='dropdown' />
                Files
            </Accordion.Title>
            <Accordion.Content active={true}>
                <List>
                    <List.Item>
                        <Table>
                            <Table.Header>
                                <Table.Row>
                                    <Table.HeaderCell className='filelist-selector'>
                                        <Checkbox 
                                            fitted
                                            onChange={(event, data) => files.map(f => onSelectionChange(f, data.checked))}
                                            checked={files.filter(f => !f.selected).length === 0}
                                            disabled={disabled}
                                        />
                                    </Table.HeaderCell>
                                    <Table.HeaderCell className='filelist-filename'>File</Table.HeaderCell>
                                    <Table.HeaderCell className='filelist-size'>Size</Table.HeaderCell>
                                    <Table.HeaderCell className='filelist-bitrate'>Bitrate</Table.HeaderCell>
                                    <Table.HeaderCell className='filelist-length'>Length</Table.HeaderCell>
                                </Table.Row>
                            </Table.Header>                                
                            <Table.Body>
                                {files.map((f, i) => 
                                    <Table.Row key={i}>
                                        <Table.Cell className='filelist-selector'>
                                            <Checkbox 
                                                fitted 
                                                onChange={(event, data) => onSelectionChange(f, data.checked)}
                                                checked={f.selected}
                                                disabled={disabled}
                                            />
                                        </Table.Cell>
                                        <Table.Cell className='filelist-filename'>{getFileName(f.filename)}</Table.Cell>
                                        <Table.Cell className='filelist-size'>{formatBytes(f.size)}</Table.Cell>
                                        <Table.Cell className='filelist-bitrate'>{f.bitRate}</Table.Cell>
                                        <Table.Cell className='filelist-length'>{formatSeconds(f.length)}</Table.Cell>
                                    </Table.Row>
                                )}
                            </Table.Body>
                        </Table>
                    </List.Item>
                </List>
            </Accordion.Content>
        </Accordion>
    </div>
);

export default FileList;
