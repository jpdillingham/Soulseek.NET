import React, { Component } from 'react';

import {
    Card,
    Button
} from 'semantic-ui-react';

import TransferList from './TransferList';

class TransferGroup extends Component {
    state = { selections: new Set() }

    onSelectionChange = (directoryName, file, selected) => {
        const { selections } = this.state;
        const obj = JSON.stringify({ directory: directoryName, filename: file.filename });
        selected ? selections.add(obj) : selections.delete(obj);

        this.setState({ selections });
    }

    isSelected = (directoryName, file) => 
        this.state.selections.has(JSON.stringify({ directory: directoryName, filename: file.filename }));

    getSelectedFiles = () => {
        const { user } = this.props;
        
        return Array.from(this.state.selections)
            .map(s => JSON.parse(s))
            .map(s => user.directories
                .find(d => d.directory === s.directory)
                .files.find(f => f.filename === s.filename))
    }

    isStateRetryable = (state) => state.includes('Completed') && state !== 'Completed, Succeeded';
    isStateCancellable = (state) => ['InProgress', 'Requested', 'Queued', 'Initializing'].find(s => s === state);
    isStateRemovable = (state) => state.includes('Completed');
    
    render = () => {
        const { user } = this.props;

        const selected = this.getSelectedFiles();
        const all = selected.length > 1 ? ' All' : '';
        
        const anyRetryable = selected.filter(f => this.isStateRetryable(f.state)).length > 0;
        const anyCancellable = selected.filter(f => this.isStateCancellable(f.state)).length > 0;
        const anyRemovable = selected.filter(f => this.isStateRemovable(f.state)).length > 0;

        return (
            <Card key={user.username} className='transfer-card' raised>
                <Card.Content>
                    <Card.Header>{user.username}</Card.Header>
                    {user.directories && user.directories
                        .map((dir, index) => 
                        <TransferList 
                            key={index} 
                            username={user.username} 
                            directoryName={dir.directory}
                            files={(dir.files || []).map(f => ({ ...f, selected: this.isSelected(dir.directory, f) }))}
                            onSelectionChange={this.onSelectionChange}
                            direction={this.props.direction}
                        />
                    )}
                </Card.Content>
                {selected && selected.length > 0 && 
                <Card.Content extra>
                    {<Button.Group>
                        {anyRetryable && 
                        <Button 
                            icon='redo' 
                            color='green' 
                            content={`Retry${all}`} 
                            onClick={() => console.log(this.groupSelectedByState(selected))}
                        />}
                        {anyRetryable && anyCancellable && <Button.Or/>}
                        {anyCancellable && 
                        <Button 
                            icon='x'
                            color='red'
                            content={`Cancel${all}`}
                            onClick={() => console.log(this.getSelectedFiles())}
                        />}
                        {(anyRetryable || anyCancellable) && anyRemovable && <Button.Or/>}
                        {anyRemovable && 
                        <Button 
                            icon='delete'
                            content={`${anyCancellable ? 'Cancel and ' : ''}Remove${all}`}
                            onClick={() => console.log(this.getSelectedFiles())}
                        />}
                    </Button.Group>}
                </Card.Content>}
            </Card>
        );
    }
}

export default TransferGroup;