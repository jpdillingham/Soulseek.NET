import React, { Component } from 'react';
import { formatSeconds, formatBytes } from './util';
import { Segment, Input, Button, Card, Table, Icon, List } from 'semantic-ui-react';
import FileList from './FileList'

class Search extends Component {
    render() {
        return (
            <Segment className='searchSegment'>
                    <Input 
                        loading={this.props.pending}
                        disabled={this.props.pending}
                        className='searchInput'
                        placeholder="Enter search phrase..."
                        onChange={this.props.onSearchPhraseChange}
                        action={!this.props.pending && { content: 'Search', onClick: this.props.search }}
                    />
            </Segment>
        )
    }
}

export default Search;
