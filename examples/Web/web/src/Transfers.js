import React, { Component } from 'react';
import axios from 'axios';

import { BASE_URL } from './constants';
import TransferGroup from './TransferGroup';

class Transfers extends Component {
    state = { fetchState: '', downloads: [], interval: undefined }

    componentDidMount = () => {
        this.fetch();
        this.setState({ interval: window.setInterval(this.fetch, 500) });
    }

    componentWillUnmount = () => {
        clearInterval(this.state.interval);
        this.setState({ interval: undefined });
    }

    fetch = () => {
        this.setState({ fetchState: 'pending' }, () => {
            axios.get(BASE_URL + `/transfers/${this.props.direction}s`)
            .then(response => this.setState({ 
                fetchState: 'complete', downloads: response.data
            }))
            .catch(err => this.setState({ fetchState: 'failed' }))
        })
    }
    
    render = () => {
        let { downloads } = this.state;

        return (
            downloads.length === 0 ? 
            <h3 className='transfer-placeholder'>
                No {this.props.direction}s
            </h3> :
            <div className='transfer-segment'>
                {downloads.map((user, index) => 
                    <TransferGroup key={index} direction={this.props.direction} user={user}/>
                )}
                <div>&nbsp;</div>
            </div>
        );
    }
}

export default Transfers;